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
	[Group("admin"), Name(Source), RequireContext(ContextType.Guild), RequireUserPermission(ChannelPermission.ManageChannels)]
	public class AdminModule : ModuleBase<SocketCommandContext>, IModuleBase
	{
		private const string Source = "Admin";

		[Command("announce"), Alias("ann"), Name(Source + "/Announce")]
		public async Task Announce([Remainder] string message)
		{
			await Context.Guild.GetQuoteChannel().SendMessageAsync(message);
		}

		[Group("modify"), Alias("mod", "set", "edit", "change"), Name(AdminModule.Source + "/" + Source)]
		public class ModifyModule : AdminModule 
		{
			private new const string Source = "Modify";

			[Command("color"), Alias("colour", "col"), Name(AdminModule.Source + "/" + Source + "/Color")]
			public async Task ModifyColorCommand([Remainder] string input)
			{
				// this is a mess. i wanna clean this up, but i have zero motivation and im tired and its almost 3am -jolk 2022-01-06
				// still a disaster, but much better than before. i really hate this method, but it works so who cares? -jolk 2022-01-06
				Match match = Regex.Match(input,
					@"((0x|#)(?<hexval>[\da-f]{1,6}))|(?<rgb>(?<r>-?\d+)[,\s]+(?<g>-?\d+)[,\s]+(?<b>-?\d+))|(?<rawint>-?\d+)",
					// this regex makes my eyes bleed but thats just because its regex. just trust me it works -jolk 2022-01-06
					RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

				if (!match.Success)
				{
					await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed($"Unable convert `{input}` to a color!").Build());
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
							await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed("All RGB values must be between 0 and 255").Build());
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
							await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed($"Raw decimal color value must be between 0 and {0xffffff:#,###}").Build());
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

						await Program.LogAsync($"Color of {Context.Guild.GetNameWithId()} changed to {newColor.ToColorString(false)}!", Source);
						break;

					case ServerData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							.WithDescription($"Server color for {Context.Guild.Name}\nis already **{newColor.ToColorString()}**");

						await Program.LogAsync($"Color of {Context.Guild.GetNameWithId()} was already {newColor.ToColorString(false)}!", Source);
						break;

					default:
						embed = EmbedHelper.GenerateErrorEmbed("Could not change color!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						await Program.LogAsync("This message should never appear. If you see this something\'s up with the announcement channel modify command.", Source, LogSeverity.Warning);
						break;
				}

				await Context.Channel.SendMessageAsync("", false, embed.Build());
			}


			[Command("announcements"), Alias("announce", "ann"), Name(AdminModule.Source + "/" + Source + "/Announcements")]
			public async Task ModifyAnnouncementChannel(SocketTextChannel channel = null)
			{
				if (channel == null)
				{
					await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed("No channel was specified!").Build());
					await Program.LogAsync("Announcement channel unchanged. No channel was specified.", Source);
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

						await Program.LogAsync($"Announcement channel of {Context.Guild.GetNameWithId()} changed to {channel.GetNameWithId()}!", Source);
						break;

					case ServerData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							 .WithDescription($"{channel.Mention} is already the announcement channel\nin {Context.Guild.Name}");

						await Program.LogAsync($"Announcement channel of {Context.Guild.GetNameWithId()} was already {channel.GetNameWithId()}!", Source);
						break;

					default:
						embed = EmbedHelper.GenerateErrorEmbed("Could not change announcement channel!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						await Program.LogAsync("This message should never appear. If you see this something\'s up with the announcement channel modify command.", Source, LogSeverity.Warning);
						break;
				}

				await Context.Channel.SendMessageAsync("", false, embed.Build());
			}

			[Command("quotes"), Alias("quote", "qt"), Name(AdminModule.Source + "/" + Source + "/Quotes")]
			public async Task ModifyQuoteChannel(SocketTextChannel channel = null)
			{
				if (channel == null)
				{
					await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed("No channel was specified!").Build());
					await Program.LogAsync("Announcement channel unchanged. No channel was specified.", Source);
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

						await Program.LogAsync($"Quote channel of {Context.Guild.GetNameWithId()} changed to {channel.GetNameWithId()}!", Source);
						break;

					case ServerData.ModificationResult.Unchanged:
						embed.WithTitle("Nothing was changed!")
							 .WithDescription($"{channel.Mention} is already the quotes channel\nin {Context.Guild.Name}");

						await Program.LogAsync($"Quote channel of {Context.Guild.GetNameWithId()} was already {channel.GetNameWithId()}!", Source);
						break;

					default:
						embed = EmbedHelper.GenerateErrorEmbed("Could not change quote channel!\n" +
							"This really should never happen.\n" +
							"If you see this message, there\'s a bug somewhere and something has gone wrong");

						await Program.LogAsync("This message should never appear. If you see this something\'s up with the quote channel modify command.", Source, LogSeverity.Warning);
						break;
				}

				await Context.Channel.SendMessageAsync("", false, embed.Build());
			}


			[Group("filter"), Alias("bannedwords", "blacklist"), Name(AdminModule.Source + "/" + ModifyModule.Source + "/" + Source)]
			public class WordFilterModule : AdminModule
			{
				private new const string Source = "Word Filter";
				private readonly Regex WordRegex = new Regex(@"(?<=\b)([a-z0-9]*)(?=\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

				[Command("add")]
				public async Task AddWords([Remainder] string input)
				{
					MatchCollection matches = WordRegex.Matches(input);
					string[] words = WordRegex.Matches(input).Select(match => match.Value).Where(str => str != "").Distinct().ToArray();

					ServerData.ModificationResult result = Context.Guild.AddFilteredWords(words);

					// this is annoying to read i think -jolk 2022-02-15
					EmbedBuilder embed = (result switch
					{
						ServerData.ModificationResult.Success => new EmbedBuilder().WithTitle("Wordlist updated!")
																				   .WithDescription($"All words have been addeded to the server\'s filters\n```{string.Join(", ", words.Select(str => $"\"{str}\""))}```")
																				   .WithColor(Context.Guild.GetColor()),
						ServerData.ModificationResult.Unchanged => new EmbedBuilder().WithTitle("Nothing was changed!")
																					 .WithDescription("All provided words are already filtered in this server!")
																					 .WithColor(Context.Guild.GetColor()),
						_ => EmbedHelper.GenerateErrorEmbed("For some reason this server can\'t be found in my database.\nThis is a bug. It should not happen")
					}).WithImageUrl(Context.Guild.IconUrl);

					await Context.Channel.SendMessageAsync("", false, embed.Build());
				}
				[Command("remove"), Alias("delete", "del")]
				public async Task RemoveWords([Remainder] string input)
				{
					MatchCollection matches = WordRegex.Matches(input);
					string[] words = WordRegex.Matches(input).Select(match => match.Value).Where(str => str != "").Distinct().ToArray();

					ServerData.ModificationResult result = Context.Guild.RemoveFilteredWords(words);

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

					await Context.Channel.SendMessageAsync("", false, embed.Build());
				}
				[Command("clear")]
				public async Task ClearWords()
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
					}).WithImageUrl(Context.Guild.IconUrl);

					await Context.Channel.SendMessageAsync("", false, embed.Build());
				}
			}
		}

		
	}
}
