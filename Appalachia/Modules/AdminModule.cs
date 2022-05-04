using Appalachia.Data;
using Appalachia.Utility;
using Appalachia.Utility.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	[Group("admin"), Alias("adm", "ad"), Name(Source), RequireContext(ContextType.Guild), RequireUserPermission(ChannelPermission.ManageChannels)]
	public class AdminModule : ModuleBase<SocketCommandContext>, IModuleBase
	{
		private const string Source = "Admin"; // we probably dont need this for anything tbh? -jolk 2022-05-03

		[Group("announce"), Alias("ann"), Name(Source)]
		public class AnnounceModule : ModuleWithHelp
		{
			private const string Source = "Announce";

			public override string ModuleName => Source;
			public override string Description => "Sends the message argument into the announcements channel if one is specified";
			public override string Usage => "<message>";

			[Command, Name(Source)]
			public async Task Announce([Remainder] string message)
			{
				await Context.Guild.GetAnnouncementChannel().SendMessageAsync(message);
			}
		}

		[Group("get"), Alias("access", "retrieve", "check"), Name(Source)]
		public class AccessModule : ModuleWithHelp
		{
			private const string Source = "Get";

			public override string ModuleName => Source;
			public override string Description => "Retrieves bot-related server properties (announcement/quotes channel and server color)\n*See wiki page for details*";
			public override string Usage => "<property>";

			[Command("color"), Alias("colour", "col"), Name(Source)]
			public async Task AccessColorCommand()
			{
				string color = new Color(Context.Guild.GetColor()).ToColorString();
				EmbedBuilder embed = new EmbedBuilder().WithTitle($"{Context.Guild.Name} Information")
													   .WithDescription($"Color for {Context.Guild.Name} is\n**{color}**")
													   .WithColor(Context.Guild.GetColor())
													   .WithThumbnailUrl(Context.Guild.IconUrl)
													   .WithFooter($"To change the color, run {Program.Config.Settings.CommandPrefix}admin set color <new_color>");

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("announcements"), Alias("announce", "ann"), Name(Source)]
			public async Task AccessAnnouncementChannel()
			{
				SocketTextChannel channel = Context.Guild.GetAnnouncementChannel();
				EmbedBuilder embed = new EmbedBuilder().WithTitle($"{Context.Guild.Name} Information")
													   .WithColor(Context.Guild.GetColor())
													   .WithThumbnailUrl(Context.Guild.IconUrl)
													   .WithFooter($"To change the announcement channel, run {Program.Config.Settings.CommandPrefix}admin set announcements <#new_channel>");

				if (channel == null)
					embed.WithDescription($"No announcement channel is set for {Context.Guild.Name}");
				else
					embed.WithDescription($"{channel.Mention} is the annoucements channel in {Context.Guild.Name}");

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("quotes"), Alias("quote", "qt"), Name(Source)]
			public async Task AccessQuoteChannel()
			{
				SocketTextChannel channel = Context.Guild.GetQuoteChannel();
				EmbedBuilder embed = new EmbedBuilder().WithTitle($"{Context.Guild.Name} Information")
													   .WithColor(Context.Guild.GetColor())
													   .WithThumbnailUrl(Context.Guild.IconUrl)
													   .WithFooter($"To change the quotes channel, run {Program.Config.Settings.CommandPrefix}admin set quotes <#new_channel>");

				if (channel == null)
					embed.WithDescription($"No quotes channel is set for {Context.Guild.Name}");
				else
					embed.WithDescription($"{channel.Mention} is the quotes channel in {Context.Guild.Name}");

				await Context.Channel.SendEmbedAsync(embed);
			}
		}

		[Group("set"), Alias("modify", "mod", "edit", "change"), Name(Source)]
		public class ModifyModule : ModuleWithHelp
		{
			private const string Source = "Set";

			public override string ModuleName => Source;
			public override string Description => "Makes changes to bot-related server properties (announcement/quotes channel and server color)\n*See wiki page for details*";
			public override string Usage => "<property> <new_value>";

			[Command("color"), Alias("colour", "col"), Name(Source)]
			public async Task ModifyColorCommand(string input)
			{
				// this is a mess. i wanna clean this up, but i have zero motivation and im tired and its almost 3am -jolk 2022-01-06
				// still a disaster, but much better than before. i really hate this method, but it works so who cares? -jolk 2022-01-06
				Match match = Regex.Match(input,
					@"((0x|#)(?<hexval>[\da-f]{1,6}))|(?<rgb>(?<r>-?\d+)[,\s]+(?<g>-?\d+)[,\s]+(?<b>-?\d+))|(?<rawint>-?\d+)",
					// this regex makes my eyes bleed but thats just because its regex. just trust me it works -jolk 2022-01-06
					RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

				if (!match.Success)
				{
					await Context.Channel.SendEmbedAsync(EmbedHelper.GenerateErrorEmbed($"Unable convert `{input}` to a color!"));
					await Program.LogAsync($"No color regex match found for \"{input}\"", Source);
				}
				else
				{
					uint value = 0;

					if (match.Groups["hexval"].Value != "")
					{
						string hexString = match.Groups["hexval"].Value;
						switch (hexString.Length)
						{
							case 2:
								hexString = hexString.Repeat(3);
								break;
							case 1:
								hexString = hexString.Repeat(6);
								break;
							default:
								break;
						}


						value = Convert.ToUInt32(hexString, 16);
					}
					else if (match.Groups["rgb"].Value != "")
					{

						try
						{
							byte r = Convert.ToByte(match.Groups["r"].Value);
							byte g = Convert.ToByte(match.Groups["g"].Value);
							byte b = Convert.ToByte(match.Groups["b"].Value);

							value = (((uint)r << 8) + g << 8) + b;
						}
						catch (OverflowException)
						{
							await Context.Channel.SendEmbedAsync(EmbedHelper.GenerateErrorEmbed("All RGB values must be between 0 and 255"));
							return;
						}
					}
					else
					{
						bool overflow = false;
						try
						{
							value = Convert.ToUInt32(match.Groups["rawint"].Value);
						}
						catch (OverflowException)
						{
							overflow = true;
						}

						if (overflow || value > 0xffffff)
						{
							await Context.Channel.SendEmbedAsync(EmbedHelper.GenerateErrorEmbed($"Raw decimal color value must be between 0 and {0xffffff:#,###}"));
							return;
						}

					}

					await ModifyColor(value);
				}
			}
			private async Task ModifyColor(uint value)
			{
				Color newColor = new Color(value);

				EmbedBuilder embed = new EmbedBuilder()
					.WithColor(newColor)
					.WithThumbnailUrl(Context.Guild.IconUrl);
				switch (Context.Guild.SetColor(newColor))
				{
					case ServerData.ModificationResult.Success:
						embed.WithTitle("Server information modified!")
							.WithDescription($"Server color for {Context.Guild.Name}\nis now **{newColor.ToColorString()}**");

						await Program.LogAsync($"Color of {Context.Guild.GetNameWithId()} changed to {newColor.ToColorString(false)}!", Source, LogSeverity.Verbose);
						break;

					case ServerData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							.WithDescription($"Server color for {Context.Guild.Name}\nis already **{newColor.ToColorString()}**");

						await Program.LogAsync($"Color of {Context.Guild.GetNameWithId()} was already {newColor.ToColorString(false)}!", Source, LogSeverity.Verbose);
						break;

					default:
						embed = EmbedHelper.GenerateErrorEmbed("Could not change color!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						await Program.LogAsync("This message should never appear. If you see this something\'s up with the announcement channel modify command.", Source, LogSeverity.Error);
						break;
				}

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("announcements"), Alias("announce", "ann"), Name(Source)]
			public async Task ModifyAnnouncementChannel(SocketTextChannel channel)
			{
				if (channel == null)
				{
					await Context.Channel.SendEmbedAsync(EmbedHelper.GenerateErrorEmbed("No channel was specified!"));
					await Program.LogAsync("Announcement channel unchanged. No channel was specified.", Source, LogSeverity.Verbose);
					return;
				}


				EmbedBuilder embed = new EmbedBuilder()
					.WithColor(Context.Guild.GetColor())
					.WithThumbnailUrl(Context.Guild.IconUrl);

				switch (Context.Guild.SetAnnouncementChannel(channel))
				{
					case ServerData.ModificationResult.Success:
						embed.WithTitle("Server information modified!")
							 .WithDescription($"Announcement channel for {Context.Guild.Name}\nis now {channel.Mention}");

						await Program.LogAsync($"Announcement channel of {Context.Guild.GetNameWithId()} changed to {channel.GetNameWithId()}!", Source, LogSeverity.Verbose);
						break;

					case ServerData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							 .WithDescription($"{channel.Mention} is already the announcement channel\nin {Context.Guild.Name}");

						await Program.LogAsync($"Announcement channel of {Context.Guild.GetNameWithId()} was already {channel.GetNameWithId()}!", Source, LogSeverity.Verbose);
						break;

					default:
						embed = EmbedHelper.GenerateErrorEmbed("Could not change announcement channel!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						await Program.LogAsync("This message should never appear. If you see this something\'s up with the announcement channel modify command.", Source, LogSeverity.Error);
						break;
				}

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("quotes"), Alias("quote", "qt"), Name(Source)]
			public async Task ModifyQuoteChannel(SocketTextChannel channel)
			{
				if (channel == null)
				{
					await Context.Channel.SendEmbedAsync(EmbedHelper.GenerateErrorEmbed("No channel was specified!"));
					await Program.LogAsync("Quote channel unchanged. No channel was specified.", Source, LogSeverity.Verbose);
					return;
				}

				EmbedBuilder embed = new EmbedBuilder()
					.WithColor(Context.Guild.GetColor())
					.WithThumbnailUrl(Context.Guild.IconUrl);

				switch (Context.Guild.SetQuoteChannel(channel))
				{
					case ServerData.ModificationResult.Success:
						embed.WithTitle("Server information modified!")
							 .WithDescription($"Quotes channel for {Context.Guild.Name}\nis now {channel.Mention}");

						await Program.LogAsync($"Quote channel of {Context.Guild.GetNameWithId()} changed to {channel.GetNameWithId()}!", Source, LogSeverity.Verbose);
						break;

					case ServerData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							 .WithDescription($"{channel.Mention} is already the quotes channel\nin {Context.Guild.Name}");

						await Program.LogAsync($"Quote channel of {Context.Guild.GetNameWithId()} was already {channel.GetNameWithId()}!", Source, LogSeverity.Verbose);
						break;

					default:
						embed = EmbedHelper.GenerateErrorEmbed("Could not change quote channel!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						await Program.LogAsync("This message should never appear. If you see this something\'s up with the quote channel modify command.", Source, LogSeverity.Error);
						break;
				}

				await Context.Channel.SendEmbedAsync(embed);
			}
		}

		// TODO: add a modifier for ShouldMatchSubstitutions -jolk 2022-05-02
		[Group("filter"), Alias("wordfilter", "bannedwords"), Name(Source)]
		public class WordFilterModule : ModuleWithHelp
		{
			private const string Source = "Filter";

			public override string ModuleName => Source;
			public override string Description => "Adds or removes words from the server's word filter\n*Enter `-A` (case-sensitive) as the first argument of the `remove` command to clear filter*";
			public override string Usage => "<add|remove|list> <word_1> [word_2 ...]";

			[Command("add"), Name(Source)]
			public async Task AddWords(params string[] words)
			{
				ServerData.ModificationResult result = Context.Guild.AddFilteredWords(words);

				// this is annoying to read i think -jolk 2022-02-15
				EmbedBuilder embed = (result switch
				{
					ServerData.ModificationResult.Success => new EmbedBuilder().WithTitle("Wordlist updated!")
																			   .WithDescription("All words have been addeded to the server\'s filters:\n" +
																							   string.Join('\n', words.Select(str => $"{str.ToUpper()[0]}||{str[1..]}||")))
																			   .WithColor(Context.Guild.GetColor()),
					ServerData.ModificationResult.Unchanged => new EmbedBuilder().WithTitle("Nothing was changed!")
																				 .WithDescription("All provided words are already filtered in this server!")
																				 .WithColor(Context.Guild.GetColor()),
					_ => EmbedHelper.GenerateErrorEmbed("For some reason this server can\'t be found in my database.\nThis is a bug. It should not happen")
				}).WithImageUrl(Context.Guild.IconUrl);

				await Context.Channel.SendEmbedAsync(embed);
			}
			[Command("remove"), Alias("delete", "del"), Name(Source)]
			public async Task RemoveWords(params string[] words)
			{
				ServerData.ModificationResult result = Context.Guild.RemoveFilteredWords(words);
				if (words[0] == "-A")
				{
					await ClearWords();
					return;
				}

				// this is annoying to read i think -jolk 2022-02-15
				EmbedBuilder embed = (result switch
				{
					ServerData.ModificationResult.Success => new EmbedBuilder().WithTitle("Wordlist updated!")
																			   .WithDescription($"All words have been removed from the server\'s filters\n```{string.Join(", ", words.Select(str => $"\"{str}\""))}```")
																			   .WithColor(Context.Guild.GetColor()),
					ServerData.ModificationResult.Unchanged => new EmbedBuilder().WithTitle("Nothing was changed!")
																				 .WithDescription("All provided words are already allowed in this server!")
																				 .WithColor(Context.Guild.GetColor()),
					_ => EmbedHelper.GenerateErrorEmbed("For some reason this server can\'t be found in my database.\nThis is a bug. It should not happen")
				}).WithImageUrl(Context.Guild.IconUrl);

				await Context.Channel.SendEmbedAsync(embed);
			}
			private async Task ClearWords()
			{
				ServerData.ModificationResult result = Context.Guild.ClearFilteredWords();

				// this is annoying to read i think -jolk 2022-02-15
				EmbedBuilder embed = (result switch
				{
					ServerData.ModificationResult.Success => new EmbedBuilder().WithTitle("Wordlist updated!")
																			   .WithDescription("Server word filter has been cleared!")
																			   .WithColor(Context.Guild.GetColor()),
					ServerData.ModificationResult.Unchanged => new EmbedBuilder().WithTitle("Nothing was changed!")
																				 .WithDescription("The server word filter was already empty!")
																				 .WithColor(Context.Guild.GetColor()),
					_ => EmbedHelper.GenerateErrorEmbed("For some reason this server can\'t be found in my database.\nThis is a bug. It should not happen")
				}).WithThumbnailUrl(Context.Guild.IconUrl);

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("list"), Alias("ls"), Name(Source)]
			public async Task ListWords([Remainder] string _ = "")
			{
				EmbedBuilder embed = new EmbedBuilder().WithTitle($"List of filtered words in {Context.Guild.Name}")
													   .WithDescription("List spoiler tagged as filters are likely to contain offensive terms\nItems are individually hidden. First letter is shown\n" +
																	   $"{string.Join('\n', Context.Guild.GetFilteredWords().Select(str => $"{str.ToUpper()[0]}||{str[1..]}||"))}")
													   .WithColor(Context.Guild.GetColor())
													   .WithThumbnailUrl(Context.Guild.IconUrl);

				await Context.Channel.SendEmbedAsync(embed);
			}
		}
	}
}
