using Appalachia.Data;
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
	[Group("rps"), RequireContext(ContextType.Guild), Name(Source)]
	public class RockPaperScissorsModule : ModuleWithHelp
	{
		public const string Source = "Rock Paper Scissors";

		public override string ModuleName => Source;
		public override string Description => "Challenge a user to a Rock Paper Scissors match";
		public override string Usage => "<@user> [first_to]";

		// theres a bunch of stuff going on here. a bunch of the message sending stuff should probably be refactored for cleanup. but like. not rn -jolk 2022-01-10
		// hey idiot. what did this mean? ive literally forgotten what this TODO was referring to. no idea if ive already done it. whelp -jolk 2022-04-27
		// i think i did the thing i was talking about? still really cant tell lmao. gonna kill the TODO tho -jolk 2022-05-10

		// might want to redo all of this with buttons instead of reactions? would certainly make life a decent bit easier. idk we'll see. just wanna get 2.0 out first -jolk 2022-04-28
		// i actually dont think i like buttons very much? idk it might make stuff easier, but im not a big fan -jolk 2022-05-11

		[Command, Name(Source)]
		public async Task RockPaperScissorsCommand(SocketGuildUser opponent = null, uint firstToScore = 1)
		{
			if (opponent == null)
			{
				await this.HelpCommand();
				return;
			}

			if (opponent.IsBot)
			{
				if (opponent.Id == Program.Client.CurrentUser.Id)
				{
					EmbedBuilder embed = new EmbedBuilder().WithTitle("Challenge accepted!")
											.WithDescription($"I accept {Context.User.Mention}\'s challenge!")
											.WithColor(Context.Guild.GetColor());

					await Context.Channel.SendEmbedAsync(embed);

					RpsSelection botSelection = (RpsSelection)(1 << Util.Rand.Next(3));
					RpsGame gameData = new RpsGame(Context.Guild.Id, Context.Channel.Id, Context.User.Id, opponent.Id, firstToScore, botSelection);
					await Program.LogAsync($"Appalachia selects [{botSelection}] in match [#{gameData.MatchId:x6}] against [{Context.User.GetFullUsername()}]", Source, LogSeverity.Verbose);

					gameData.AddToDatabase();
					IMessage message = await Program.SendBotSelectionMessage(Context.Channel, Context.User, gameData);

					await message.AddRpsReactionsAsync();
				}
				else
					await Context.Channel.SendErrorMessageAsync("I am the only bot smart enough to play this game.\nsorry");
			}
			else if (Context.User.Id == opponent.Id)
				await Context.Channel.SendErrorMessageAsync("You cannot challenge yourself!");
			else
			{
				EmbedBuilder embed = new EmbedBuilder().WithTitle($"{opponent.Nickname ?? opponent.Username}, do you accept the challenge?")
										.WithDescription($"{Context.User.Mention} challenges {opponent.Mention} to a Rock Paper Scissors match!{(firstToScore > 1 ? $"\n(First to {firstToScore} wins)" : "")}")
										.WithColor(Context.Guild.GetColor());


				IMessage message = await Context.Channel.SendEmbedAsync(embed);
				new RpsChallenge(Context.Guild.Id, Context.Channel.Id, Context.User.Id, opponent.Id, firstToScore).AddToDatabase(message.Id);

				await message.AddReactionAsync(Reactions.RpsAccept);
				await message.AddReactionAsync(Reactions.RpsDeny);
			}
		}
		[Command("leaderboard"), Alias("lb", "scores"), Name(Source + " lb")]
		public async Task RockPaperScissorsLeaderboard(SocketGuildUser userFilter = null)
		{
			if (userFilter == null)
			{// i actually really dont like this code, but it works so im leaving it at least for now -jolk 2022-03-22
				KeyValuePair<ulong, Server.UserScore>[] leaderboard = Context.Guild.GetRpsLeaderboard();

				(int rank, string user, string elo, string wins, string losses, string winRate)[] userDataStrings = new (int, string, string, string, string, string)[leaderboard.Length];
				// this is kinda gross but i think it might be the easiest way? -jolk 2022-03-15

				int rank = 1, nameSpacing = 4, eloSpacing = 3, winsSpacing = 1, lossesSpacing = 1, winRateSpacing = 5;
				for (int i = 0; i < leaderboard.Length; i++)
				{
					KeyValuePair<ulong, Server.UserScore> pair = leaderboard[i];
					if (i > 0 && pair.Value.Elo != leaderboard[i - 1].Value.Elo)
						rank = i + 1;

					SocketGuildUser user = Context.Guild.GetUser(pair.Key);
					userDataStrings[i] = (rank, user.GetFullUsername(), pair.Value.Elo.ToString(), pair.Value.Wins.ToString(), pair.Value.Losses.ToString(), Math.Round(pair.Value.WinRate, 4).ToString("#.0000"));


					// this is awful. please come up with a better way to do this thank -jolk 2022-03-17
					if (userDataStrings[i].user.Length > nameSpacing)
						nameSpacing = userDataStrings[i].user.Length;

					if (userDataStrings[i].elo.Length > eloSpacing)
						eloSpacing = userDataStrings[i].elo.Length;

					if (userDataStrings[i].wins.Length > winsSpacing)
						winsSpacing = userDataStrings[i].wins.Length;

					if (userDataStrings[i].losses.Length > lossesSpacing)
						lossesSpacing = userDataStrings[i].losses.Length;

					if (userDataStrings[i].winRate.Length > winRateSpacing)
						winRateSpacing = userDataStrings[i].winRate.Length;
				}

				int rankSpacing = (userDataStrings[^1].rank / 10) + 1;

				string output = string.Format($"```{{0, -{rankSpacing}}}. │ {{1, -{nameSpacing}}} │ {{2, -{eloSpacing}}} │ {{3, {winsSpacing}}} │ {{4, {lossesSpacing}}} │ {{5, {winRateSpacing}}}",
					"#", "USER", "ELO", "W", "L", "W/L");
				output += $"\n{"═".Repeat(rankSpacing)}══╪═{"═".Repeat(nameSpacing)}═╪═{"═".Repeat(eloSpacing)}═╪═{"═".Repeat(winsSpacing)}═╪═{"═".Repeat(lossesSpacing)}═╪═{"═".Repeat(winRateSpacing)}";

				bool top3 = false;
				foreach ((int rank, string user, string elo, string wins, string losses, string winRate) data in userDataStrings)
				{
					if (!top3 && data.rank > 3)
					{
						output += $"\n{"┄".Repeat(rankSpacing)}┄┄┼┄{"┄".Repeat(nameSpacing)}┄┼┄{"┄".Repeat(eloSpacing)}┄┼┄{"┄".Repeat(winsSpacing)}┄┼┄{"┄".Repeat(lossesSpacing)}┄┼┄{"┄".Repeat(winRateSpacing)}";
						top3 = true;
					}

					output += string.Format($"\n{{0, -{rankSpacing}}}. │ {{1, -{nameSpacing}}} │ {{2, {eloSpacing}}} │ {{3, {winsSpacing}}} │ {{4, {lossesSpacing}}} │ {{5, {winRateSpacing}}}",
						data.rank, data.user, data.elo, data.wins, data.losses, data.winRate);
				}

				EmbedBuilder embed = new EmbedBuilder().WithTitle("Rock Paper Scissors Leaderboard")
					.WithDescription($"{output}```")
					.WithThumbnailUrl(Context.Guild.IconUrl)
					.WithColor(Context.Guild.GetColor());

				await Context.Channel.SendEmbedAsync(embed);
			}
			else
			{
				Server.UserScore userScore = userFilter.GetGuildRpsScore();
				EmbedBuilder embed = new EmbedBuilder().WithTitle("Rock Paper Scissors Stats")
										   .WithDescription($"Stats for {userFilter.Mention}\n")
										   .WithFields(new EmbedFieldBuilder[]
										   {
											   new EmbedFieldBuilder().WithName("Rank in Server").WithValue($"{userFilter.GetGuildRpsRank().ToOrdinal()} ({userScore.Elo})").WithIsInline(false),
											   new EmbedFieldBuilder().WithName("Wins").WithValue(userScore.Wins).WithIsInline(true),
											   new EmbedFieldBuilder().WithName("Losses").WithValue(userScore.Losses).WithIsInline(true),
											   new EmbedFieldBuilder().WithName("Win Rate").WithValue($"{userScore.WinRate * 100 : 0.0}%").WithIsInline(true)
										   })
										   .WithColor(Context.Guild.GetColor())
										   .WithThumbnailUrl(userFilter.GetGuildOrDefaultAvatarUrl());

				await Context.Channel.SendEmbedAsync(embed);
			}
		}

		[Command("leaderboard"), Alias("lb", "scores"), Name(Source + "/lb")]
		public async Task RockPaperScissorsLeaderboard([Remainder] string userArg)
		{
			if (Context.Guild.TryGetUser(userArg, out SocketGuildUser user))
				await RockPaperScissorsLeaderboard(user); // whats the worst that can happen?? -jolk 2022-03-22
			else
				await Context.Channel.SendErrorMessageAsync($"Could not find user \"{userArg}\"");
		}
	}
}
