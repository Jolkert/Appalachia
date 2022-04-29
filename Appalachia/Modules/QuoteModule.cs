using Appalachia.Utility;
using Appalachia.Utility.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	[Group("quote"), Alias("qt"), RequireContext(ContextType.Guild), Name(Source)]
	public class QuoteModule : ModuleWithHelp
	{
		private const string Source = "Quote";

		public override string ModuleName => Source;
		public override string Description => "If a channel in the server is designated as the quotes channel, get a random quote";
		public override string Usage => "[@user*]";

		[Command, Name(Source)]
		public async Task RandomQuote(SocketGuildUser userFilter = null, [Remainder] string _ = "")
		{
			EmbedBuilder embed;

			SocketTextChannel quoteChannel = Context.Guild.GetQuoteChannel();
			if (quoteChannel == null)
				embed = await GetNotSpecifiedEmbed();
			else
			{
				List<IMessage> quotes = await GetQuotes(quoteChannel, userFilter);

				IMessage selectedQuote = quotes[Util.Rand.Next(quotes.Count)];
				SocketGuildUser quotee = Context.Guild.GetUser(selectedQuote.MentionedUserIds.FirstOrDefault());

				embed = new EmbedBuilder().WithTitle("Here's a thing someone said!")
							  .WithDescription(selectedQuote.Content)
							  .WithColor(Context.Guild.GetColor())
							  .WithThumbnailUrl(quotee?.GetGuildOrDefaultAvatarUrl())
							  .WithUrl($"{selectedQuote.GetJumpUrl()}")
							  .WithTimestamp(selectedQuote.Timestamp);
			}

			await Context.Channel.SendEmbedAsync(embed);
		}
		[Command, Name(Source)]
		public async Task RandomQuote([Remainder] string userArg)
		{
			if (Context.Guild.TryGetUser(userArg, out SocketGuildUser user))
				await RandomQuote(user);
			else
				await Context.Channel.SendEmbedAsync(EmbedHelper.GenerateErrorEmbed($"Could not find user \"{userArg}\""));
		}

		[Command("leaderboard"), Alias("lb", "%"), Name(Source + "/lb")]
		public async Task QuoteLeaderboard(SocketGuildUser userFilter = null, [Remainder] string _ = "")
		{
			EmbedBuilder embed = new EmbedBuilder().WithColor(Context.Guild.GetColor());

			SocketTextChannel quoteChannel = Context.Guild.GetQuoteChannel();
			if (quoteChannel == null)
				embed = await GetNotSpecifiedEmbed();
			else
			{
				embed.WithTitle("Quote Stats");

				List<IMessage> quotes = await GetQuotes(quoteChannel);
				if (userFilter == null)
				{
					Dictionary<ulong, int> scoresByUser = new Dictionary<ulong, int>();
					int totalQuotes = 0;

					foreach (IMessage quote in quotes)
					{
						totalQuotes++;
						foreach (ulong id in quote.MentionedUserIds.Distinct())
						{
							if (scoresByUser.ContainsKey(id))
								scoresByUser[id]++;
							else if (Context.Guild.GetUser(id) != null)
								scoresByUser.Add(id, 1);
						}
					}

					string embedDescription = $"Server quote leaderboard for {Context.Guild.Name} ({totalQuotes} total):";
					foreach (KeyValuePair<ulong, int> user in scoresByUser.ToList().OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key))
						embedDescription += $"\n{Context.Guild.GetUser(user.Key)?.Mention ?? "[USER NOT FOUND]"} - {(float)user.Value / totalQuotes * 100:#.#}% ({user.Value})";
					// loop is impossible to parse i know. the syntactic sugar was too tempting. couldnt help myself. i am sorry in advance for when you have to read this again and spend minutes parsing it -jolk 2022-01-04
					// also with the way ive now changed GetQuotes and just how this whole thing works in general, the null propagation/coalesence *shouldnt* actually be necessary? but just in case -jolk 2022-01-05

					embed.WithDescription(embedDescription);
					embed.WithThumbnailUrl(Context.Guild.IconUrl);
				}
				else
				{
					int filterCount = quotes.Where(msg => msg.MentionedUserIds.Contains(userFilter.Id)).Count();

					embed.WithDescription($"{userFilter.Mention} is responsible for\n{filterCount} quote{(filterCount != 1 ? "s" : "")} ({(float)filterCount / quotes.Count * 100:#.#}%) in {Context.Guild.Name}");
					embed.WithThumbnailUrl(userFilter.GetGuildOrDefaultAvatarUrl());
				}
			}

			await Context.Channel.SendEmbedAsync(embed);
		}
		[Command("leaderboard"), Alias("lb", "%"), Name(Source + "/lb")]
		public async Task QuoteLeaderboard([Remainder] string userArg)
		{
			if (Context.Guild.TryGetUser(userArg, out SocketGuildUser user))
				await QuoteLeaderboard(user);
			else
				await Context.Channel.SendEmbedAsync(EmbedHelper.GenerateErrorEmbed($"Could not find user \"{userArg}\""));
		}

		private static async Task<List<IMessage>> GetQuotes(SocketTextChannel quoteChannel, IUser user = null)
		{// Try optimization at your own peril. I've tried so many things. I think either I just dont know how to properly use IAsyncEnumerable, or discord just be slow. prob both -jolk 2022-01-12
			List<IMessage> quotes = new List<IMessage>();
			await foreach (IReadOnlyCollection<IMessage> subcontainer in quoteChannel.GetMessagesAsync(int.MaxValue))
				foreach (IMessage message in subcontainer.Where(msg => msg.MentionedUserIds.Count > 0 && (user == null || msg.MentionedUserIds.Contains(user.Id))))
					quotes.Add(message);

			return quotes;
		}
		private async Task<EmbedBuilder> GetNotSpecifiedEmbed()
		{
			await Program.LogAsync($"No quote channel in server [{Context.Guild?.GetNameWithId() ?? Context.User.GetFullUsername()}]", Source);
			return EmbedHelper.GenerateErrorEmbed("This server has no defined quotes channel!");
		}
	}
}
