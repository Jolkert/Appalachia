using Appalachia.Data;
using Appalachia.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	[Group("admin"), Alias("adm", "ad"), Name(Name), RequireContext(ContextType.Guild), RequireUserPermission(ChannelPermission.ManageChannels)]
	public class AdminModule : ModuleBase<SocketCommandContext>, IModuleBase
	{
		private const string Name = "Admin"; // we probably dont need this for anything tbh? -jolk 2022-05-03

		[Group("announce"), Alias("ann"), Name(Name)]
		public class AnnounceModule : ModuleWithHelp
		{
			private const string Name = "Announce";

			public override string ModuleName => Name;
			public override string Description => "Sends the message argument into the announcements channel if one is specified";
			public override string Usage => "<message>";

			[Command, Name(Name)]
			public async Task Announce([Remainder] string message)
			{
				await Context.Guild.GetAnnouncementChannel().SendMessageAsync(message);
			}
		}

		[Group("get"), Alias("access", "retrieve", "check"), Name(Name)]
		public class AccessModule : ModuleWithHelp
		{
			private const string Name = "Get";

			public override string ModuleName => Name;
			public override string Description => "Retrieves bot-related server properties (announcement/quotes channel and server color)\n*See wiki page for details*";
			public override string Usage => "<property>";

			[Command("color"), Alias("colour", "col"), Name(Name)]
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

			[Command("announcements"), Alias("announce", "ann"), Name(Name)]
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

			[Command("quotes"), Alias("quote", "qt"), Name(Name)]
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

			[Command("defaultrole"), Alias("defrole"), Name(Name)]
			public async Task AccessDefaultRole()
			{
				SocketRole role = Context.Guild.GetDefaultRole();
				EmbedBuilder embed = new EmbedBuilder().WithTitle($"{Context.Guild.Name} Information")
													   .WithColor(Context.Guild.GetColor())
													   .WithThumbnailUrl(Context.Guild.IconUrl)
													   .WithFooter($"To change the default role, run {Program.Config.Settings.CommandPrefix}admin set defaultrole <#new_channel>");

				if (role == null)
					embed.WithDescription($"No default role is set for {Context.Guild.Name}");
				else
					embed.WithDescription($"{role.Mention} is the default role in {Context.Guild.Name}");

				await Context.Channel.SendEmbedAsync(embed);
			}
		}

		[Group("set"), Alias("modify", "mod", "edit", "change"), Name(Name)]
		public class ModifyModule : ModuleWithHelp
		{
			private const string Name = "Set";

			public override string ModuleName => Name;
			public override string Description => "Makes changes to bot-related server properties (announcement/quotes channel and server color)\n*See wiki page for details*";
			public override string Usage => "<property> <new_value>";

			[Command("color"), Alias("colour", "col"), Name(Name)]
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
					await Context.Channel.SendErrorMessageAsync($"Unable convert `{input}` to a color!");
					Program.Logger.Info($"No color regex match found for \"{input}\"");
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


						value = Convert.ToUInt32(16);
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
							await Context.Channel.SendErrorMessageAsync("All RGB values must be between 0 and 255");
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
							await Context.Channel.SendErrorMessageAsync($"Raw decimal color value must be between 0 and {0xffffff:#,###}");
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
					case GuildData.ModificationResult.Success:
						embed.WithTitle("Server information modified!")
							.WithDescription($"Server color for {Context.Guild.Name}\nis now **{newColor.ToColorString()}**");

						Program.Logger.Verbose($"Color of {Context.Guild.GetNameWithId()} changed to {newColor.ToColorString(false)}!");
						break;

					case GuildData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							.WithDescription($"Server color for {Context.Guild.Name}\nis already **{newColor.ToColorString()}**");

						Program.Logger.Verbose($"Color of {Context.Guild.GetNameWithId()} was already {newColor.ToColorString(false)}!");
						break;

					default:
						await Context.Channel.SendErrorMessageAsync("Could not change color!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						Program.Logger.Error("This message should never appear. If you see this something\'s up with the announcement channel modify command.");
						return;
				}

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("announcements"), Alias("announce", "ann"), Name(Name)]
			public async Task ModifyAnnouncementChannel(SocketTextChannel channel)
			{
				if (channel == null)
				{
					await Context.Channel.SendErrorMessageAsync("No channel was specified!");
					Program.Logger.Verbose("Announcement channel unchanged. No channel was specified.");
					return;
				}


				EmbedBuilder embed = new EmbedBuilder()
					.WithColor(Context.Guild.GetColor())
					.WithThumbnailUrl(Context.Guild.IconUrl);

				switch (Context.Guild.SetAnnouncementChannel(channel))
				{
					case GuildData.ModificationResult.Success:
						embed.WithTitle("Server information modified!")
							 .WithDescription($"Announcement channel for {Context.Guild.Name}\nis now {channel.Mention}");

						Program.Logger.Verbose($"Announcement channel of {Context.Guild.GetNameWithId()} changed to {channel.GetNameWithId()}!");
						break;

					case GuildData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							 .WithDescription($"{channel.Mention} is already the announcement channel\nin {Context.Guild.Name}");

						Program.Logger.Verbose($"Announcement channel of {Context.Guild.GetNameWithId()} was already {channel.GetNameWithId()}!");
						break;

					default:
						await Context.Channel.SendErrorMessageAsync("Could not change announcement channel!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						Program.Logger.Error("This message should never appear. If you see this something\'s up with the announcement channel modify command.");
						return;
				}

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("quotes"), Alias("quote", "qt"), Name(Name)]
			public async Task ModifyQuoteChannel(SocketTextChannel channel)
			{
				if (channel == null)
				{
					await Context.Channel.SendErrorMessageAsync("No channel was specified!");
					Program.Logger.Verbose("Quote channel unchanged. No channel was specified.");
					return;
				}

				EmbedBuilder embed = new EmbedBuilder()
					.WithColor(Context.Guild.GetColor())
					.WithThumbnailUrl(Context.Guild.IconUrl);

				switch (Context.Guild.SetQuoteChannel(channel))
				{
					case GuildData.ModificationResult.Success:
						embed.WithTitle("Server information modified!")
							 .WithDescription($"Quotes channel for {Context.Guild.Name}\nis now {channel.Mention}");

						Program.Logger.Verbose($"Quote channel of {Context.Guild.GetNameWithId()} changed to {channel.GetNameWithId()}!");
						break;

					case GuildData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							 .WithDescription($"{channel.Mention} is already the quotes channel\nin {Context.Guild.Name}");

						Program.Logger.Verbose($"Quote channel of {Context.Guild.GetNameWithId()} was already {channel.GetNameWithId()}!");
						break;

					default:
						await Context.Channel.SendErrorMessageAsync("Could not change quote channel!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						Program.Logger.Error("This message should never appear. If you see this something\'s up with the quote channel modify command.");
						return;
				}

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("defaultrole"), Alias("defrole"), Name(Name)]
			public async Task ModifyDefaultRole(SocketRole role)
			{
				EmbedBuilder embed = new EmbedBuilder()
					.WithColor(Context.Guild.GetColor())
					.WithThumbnailUrl(Context.Guild.IconUrl);

				switch (Context.Guild.SetDefaultRole(role))
				{
					case GuildData.ModificationResult.Success:
						embed.WithTitle("Server information modified!")
							 .WithDescription($"Default role for {Context.Guild.Name}\nis now {role.Mention}");

						Program.Logger.Verbose($"Default role of {Context.Guild.GetNameWithId()} changed to {role.GetNameWithId()}!");
						break;

					case GuildData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							 .WithDescription($"{role.Mention} is already the default role\nin {Context.Guild.Name}");

						Program.Logger.Verbose($"Default role of {Context.Guild.GetNameWithId()} was already {role.GetNameWithId()}!");
						break;

					default:
						await Context.Channel.SendErrorMessageAsync("Could not change default role!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						Program.Logger.Error("This message should never appear. If you see this something\'s up with the quote channel modify default role.");
						return;
				}

				await Context.Channel.SendEmbedAsync(embed);
			}
		}

		// TODO: add a modifier for ShouldMatchSubstitutions -jolk 2022-05-02
		[Group("filter"), Alias("wordfilter", "bannedwords"), Name(Name)]
		public class WordFilterModule : ModuleWithHelp
		{
			private const string Name = "Filter";

			public override string ModuleName => Name;
			public override string Description => "Adds or removes words from the server's word filter\n*Enter `-A` (case-sensitive) as the first argument of the `remove` command to clear filter*";
			public override string Usage => "<add|remove|list> <word_1> [word_2 ...]";

			[Command("add"), Name(Name)]
			public async Task AddWords(params string[] words)
			{
				GuildData.ModificationResult result = Context.Guild.AddFilteredWords(words);

				EmbedBuilder embed;
				switch (result)
				{
					case GuildData.ModificationResult.Success:
						embed = new EmbedBuilder().WithTitle("Wordlist updated!")
												  .WithDescription("All words have been addeded to the server\'s filters:\n" + string.Join('\n', words.Select(str => $"{str.ToUpper()[0]}||{str[1..]}||")))
												  .WithColor(Context.Guild.GetColor());
						break;
					case GuildData.ModificationResult.Unchanged:
						embed = new EmbedBuilder().WithTitle("Nothing was changed!")
												  .WithDescription("All provided words are already filtered in this server!")
												  .WithColor(Context.Guild.GetColor());
						break;
					default:
						await Context.Channel.SendErrorMessageAsync("For some reason this server can\'t be found in my database.\nThis is a bug. It should not happen");
						return;
				}
				embed.WithImageUrl(Context.Guild.IconUrl);

				await Context.Channel.SendEmbedAsync(embed);
			}
			[Command("remove"), Alias("delete", "del"), Name(Name)]
			public async Task RemoveWords(params string[] words)
			{
				GuildData.ModificationResult result = Context.Guild.RemoveFilteredWords(words);
				if (words[0] == "-A")
				{
					await ClearWords();
					return;
				}

				EmbedBuilder embed;
				switch (result)
				{
					case GuildData.ModificationResult.Success:
						embed = new EmbedBuilder().WithTitle("Wordlist updated!")
													.WithDescription($"All words have been removed from the server\'s filters\n```{string.Join(", ", words.Select(str => $"\"{str}\""))}```")
												  .WithColor(Context.Guild.GetColor());
						break;
					case GuildData.ModificationResult.Unchanged:
						embed = new EmbedBuilder().WithTitle("Nothing was changed!")
												  .WithDescription("All provided words are already allowed in this server!")
												  .WithColor(Context.Guild.GetColor());
						break;
					default:
						await Context.Channel.SendErrorMessageAsync("For some reason this server can\'t be found in my database.\nThis is a bug. It should not happen");
						return;
				}
				embed.WithImageUrl(Context.Guild.IconUrl);

				await Context.Channel.SendEmbedAsync(embed);
			}
			private async Task ClearWords()
			{
				GuildData.ModificationResult result = Context.Guild.ClearFilteredWords();

				EmbedBuilder embed;
				switch (result)
				{
					case GuildData.ModificationResult.Success:
						embed = new EmbedBuilder().WithTitle("Wordlist updated!")
										  .WithDescription("Server word filter has been cleared!")
										  .WithColor(Context.Guild.GetColor());
						break;
					case GuildData.ModificationResult.Unchanged:
						embed = new EmbedBuilder().WithTitle("Nothing was changed!")
												  .WithDescription("The server word filter was already empty!")
												  .WithColor(Context.Guild.GetColor());
						break;
					default:
						await Context.Channel.SendErrorMessageAsync("For some reason this server can\'t be found in my database.\nThis is a bug. It should not happen");
						return;
				}
				embed.WithThumbnailUrl(Context.Guild.IconUrl);

				await Context.Channel.SendEmbedAsync(embed);
			}

			[Command("list"), Alias("ls"), Name(Name)]
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
