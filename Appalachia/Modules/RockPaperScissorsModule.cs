using Appalachia.Data;
using Appalachia.Extensions;
using Appalachia.Services;
using Appalachia.Utility;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Appalachia.Modules;

[Group("rps"), RequireContext(ContextType.Guild), Name(Name)]
public class RockPaperScissorsModule : ModuleWithHelp
{
	public const string Name = "Rock Paper Scissors";

	public override string ModuleName => Name;
	public override string Description => "Challenge a user to a Rock Paper Scissors match";
	public override string Usage => "<@user> [first_to]";

	// theres a bunch of stuff going on here. a bunch of the message sending stuff should probably be refactored for cleanup. but like. not rn -jolk 2022-01-10
	// hey idiot. what did this mean? ive literally forgotten what this TODO was referring to. no idea if ive already done it. whelp -jolk 2022-04-27
	// i think i did the thing i was talking about? still really cant tell lmao. gonna kill the TODO tho -jolk 2022-05-10

	// might want to redo all of this with buttons instead of reactions? would certainly make life a decent bit easier. idk we'll see. just wanna get 2.0 out first -jolk 2022-04-28
	// i actually dont think i like buttons very much? idk it might make stuff easier, but im not a big fan -jolk 2022-05-11

	[Command, Name(Name)]
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
				Program.Logger.Verbose($"Appalachia selects [{botSelection}] in match [#{gameData.MatchId:x6}] against [{Context.User.GetFullUsername()}]");

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

	[Command("leaderboard"), Alias("lb", "scores"), Name(Name + " lb")]
	public async Task RockPaperScissorsLeaderboard(SocketGuildUser userFilter = null)
	{
		if (userFilter is null)
			await SendLeaderboard();
		else
			await SendUserScore(userFilter);
	}

	private async Task SendLeaderboard()
	{
		ScoreData[] leaderboard = (from entry in Context.Guild.GetRpsLeaderboard()
								   select ScoreData.FromScore(Context.Guild, entry))
								   .ToArray();

		int currentRank = 1;
		SpacingData spacings = new SpacingData();
		for (int i = 0; i < leaderboard.Length; i++)
		{
			// set rank with elo ties ranked the same
			if (i > 0 && leaderboard[i].Elo != leaderboard[i - 1].Elo)
				currentRank = i + 1;
			leaderboard[i].Rank = currentRank;

			// get correct spacings
			spacings = spacings.KeepMax(new SpacingData(leaderboard[i]));
		}
		int rankSpacing = (leaderboard[^1].Rank / 10) + 1;

		int stringSize = 6 + (rankSpacing + spacings.Username +  spacings.Elo + spacings.Wins + spacings.Losses + spacings.WinRate + 17) * (leaderboard.Length + (leaderboard[^1].Rank > 3 ? 1 : 0) + 2);
		// 6 for backticks; 17 breaks down to: 2/spacing except winrate (10) + 1 winrate spacing + 1/vertical bar (5) + 1 newline; rest should be pretty self-explanatory? I know it sucks -jolk 2022-08-26

		string format = $"{{0, {-rankSpacing}}}. │ {{1, {-spacings.Username}}} | {{2, {spacings.Elo}}} │ {{3, {spacings.Wins}}} │ {{4, {spacings.Losses}}} │ {{5, {spacings.WinRate}}}";

		StringBuilder displayBuilder = new StringBuilder("```", stringSize)
										  .AppendFormat(format, "#", "USER", "ELO", "W", "L", "W/L")
										  .Append('\n')
										  .Append(GenerateHorizontalSeparator('═', '╪', rankSpacing + 2, spacings.Username + 2, spacings.Elo + 2, spacings.Wins + 2, spacings.Losses + 2, spacings.WinRate + 1))
										  .Append('\n');

		bool topThreeLinePlaced = false;
		foreach (ScoreData score in leaderboard)
		{
			if (!topThreeLinePlaced && score.Rank > 3)
			{
				displayBuilder.Append(GenerateHorizontalSeparator('┄', '┼', rankSpacing + 2, spacings.Username + 2, spacings.Elo + 2, spacings.Wins + 2, spacings.Losses + 2, spacings.WinRate + 1))
							  .Append('\n');
				topThreeLinePlaced = true;
			}

			displayBuilder.AppendFormat(format, score.Rank, score.Username, score.Elo, score.Wins, score.Losses, score.WinRate)
						  .Append('\n');
		}
		displayBuilder.Append("```");

		EmbedBuilder embed = new EmbedBuilder().WithTitle("Rock Paper Scissors Leaderboard")
								.WithDescription(displayBuilder.ToString())
								.WithThumbnailUrl(Context.Guild.IconUrl)
								.WithColor(Context.Guild.GetColor());

		await Context.Channel.SendEmbedAsync(embed);
	}
	private static StringBuilder GenerateHorizontalSeparator(char horizontal, char vertical, params int[] spacings)
	{
		StringBuilder builder = new StringBuilder(spacings.Sum() + spacings.Length - 1);
		for (int i = 0; i < spacings.Length; i++)
		{
			builder.Append(horizontal.Repeat(spacings[i]));
			if (i < spacings.Length - 1)
				builder.Append(vertical);
		}

		return builder;
	}

	private async Task SendUserScore(SocketGuildUser user)
	{
		Guild.UserScore userScore = user.GetGuildRpsScore();
		EmbedBuilder embed = new EmbedBuilder().WithTitle("Rock Paper Scissors Stats")
											   .WithDescription($"Stats for {user.Mention}\n")
											   .WithFields(new EmbedFieldBuilder[]
											   {
												   new EmbedFieldBuilder().WithName("Rank in Server").WithValue($"{user.GetGuildRpsRank().ToOrdinal()} ({userScore.Elo})").WithIsInline(false),
												   new EmbedFieldBuilder().WithName("Wins").WithValue(userScore.Wins).WithIsInline(true),
												   new EmbedFieldBuilder().WithName("Losses").WithValue(userScore.Losses).WithIsInline(true),
												   new EmbedFieldBuilder().WithName("Win Rate").WithValue($"{userScore.WinRate * 100 : 0.0}%").WithIsInline(true)
											   })
											   .WithColor(Context.Guild.GetColor())
											   .WithThumbnailUrl(user.GetGuildOrDefaultAvatarUrl());

		await Context.Channel.SendEmbedAsync(embed);
	}

	[Command("leaderboard"), Alias("lb", "scores"), Name(Name + "/lb")]
	public async Task RockPaperScissorsLeaderboard([Remainder] string userArg)
	{
		if (Context.Guild.TryGetUser(userArg, out SocketGuildUser user))
			await RockPaperScissorsLeaderboard(user); // whats the worst that can happen?? -jolk 2022-03-22
		else
			await Context.Channel.SendErrorMessageAsync($"Could not find user \"{userArg}\"");
	}

	private struct ScoreData
	{
		public int Rank { get; set; }
		public string Username { get; }
		public string Elo { get; }
		public string Wins { get; }
		public string Losses { get; }
		public string WinRate { get; }

		private ScoreData(int rank, string username, int elo, int wins, int losses, string winrate)
		{
			Rank = rank;
			Username = username;
			Elo = elo.ToString();
			Wins = wins.ToString();
			Losses = losses.ToString();
			WinRate = winrate;
		}

		public static ScoreData FromScore(SocketGuild guild, KeyValuePair<ulong, Guild.UserScore> pair) => FromScore(guild, pair.Key, pair.Value);
		public static ScoreData FromScore(SocketGuild guild, ulong userId, Guild.UserScore score)
		{
			return new ScoreData(-1, guild.GetUser(userId).GetFullUsername(), score.Elo, score.Wins, score.Losses, Math.Round(score.WinRate, 4).ToString("#.0000"));
		}

		public SpacingData GetDigitCounts()
		{
			return new SpacingData(this);
		}
	}
	private struct SpacingData
	{
		public int Username { get; }
		public int Elo { get; }
		public int Wins { get; }
		public int Losses { get; }
		public int WinRate { get; }

		public SpacingData(int username, int elo, int wins, int losses, int winRate)
		{
			Username = username;
			Elo = elo;
			Wins = wins;
			Losses = losses;
			WinRate = winRate;
		}
		public SpacingData(ScoreData data)
		{
			Username = data.Username.Length;
			Elo = data.Elo.Length;
			Wins = data.Wins.Length;
			Losses = data.Losses.Length;
			WinRate = data.WinRate.Length;
		}

		public SpacingData KeepMax(SpacingData other)
		{
			return new SpacingData(
				Math.Max(this.Username, other.Username),
				Math.Max(this.Elo, other.Elo),
				Math.Max(this.Wins, other.Wins),
				Math.Max(this.Losses, other.Losses),
				Math.Max(this.WinRate, other.WinRate));
		}
		public int[] ToArray()
		{
			return new int[] { Username, Elo, Wins, Losses, WinRate };
		}
	}
}