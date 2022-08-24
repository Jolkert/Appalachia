using Appalachia.Data;
using Appalachia.Utility;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Appalachia.Utility.Util;

namespace Appalachia.Extensions;

/* Extension methods for accessing and modifying RPS data
 * use these instead of Util.Rps instance methods thanks
 * -jolk 2022-07-08
 */
public static class RpsExtensions
{
	private static readonly Dictionary<string, ReactionStatus> StatusByReact = new Dictionary<string, ReactionStatus>()
	{
		{Reactions.RpsAccept.Name, ReactionStatus.RpsAccept},
		{Reactions.RpsDeny.Name, ReactionStatus.RpsDeny},

		{Reactions.RpsRock.Name, ReactionStatus.RpsRock},
		{Reactions.RpsPaper.Name, ReactionStatus.RpsPaper},
		{Reactions.RpsScissors.Name, ReactionStatus.RpsScissors}
	};

	public static void AddToDatabase(this RpsGame game)
	{// I really dont know why the method doesnt just take only the game and get the matchId from there? whatever -jolk 2022-02-16
		Rps.AddGame(game.MatchId, game);
	}
	public static void AddToDatabase(this RpsChallenge challenge, ulong messageId)
	{
		Rps.AddChallenge(messageId, challenge);
	}
	public static void RemoveFromDatabase(this RpsGame game)
	{
		Rps.RemoveGame(game.MatchId);
	}
	public static uint ParseGameId(this IEmbed embed)
	{
		string footer = embed?.Footer?.Text;
		return footer != null && footer.Contains("Match ID: ") ? Convert.ToUInt32(footer[^6..], 16) : 0;
	}

	public static async Task AddRpsReactionsAsync(this IMessage message)
	{
		await message.AddReactionAsync(Reactions.RpsRock);
		await message.AddReactionAsync(Reactions.RpsPaper);
		await message.AddReactionAsync(Reactions.RpsScissors);
	}

	public static uint IncrementChallengerScore(this RpsGame game)
	{
		return Rps.IncrementChallengerScore(game.MatchId);
	}
	public static uint IncrementOpponentScore(this RpsGame game)
	{
		return Rps.IncrementOpponentScore(game.MatchId);
	}
	public static void IncrementRound(this RpsGame game)
	{
		Rps.IncrementRound(game.MatchId);
	}

	public static void SetChallengerSelection(this RpsGame game, RpsSelection selection)
	{
		Rps.SetChallengerSelection(game.MatchId, selection);
	}
	public static void SetOpponentSelection(this RpsGame game, RpsSelection selection)
	{
		Rps.SetOpponentSelection(game.MatchId, selection);
	}
	public static void ResetSelections(this RpsGame game, bool isBotMatch = false)
	{
		Rps.ResetSelections(game.MatchId, isBotMatch);
	}

	public static ReactionStatus GetStatus(this SocketReaction reaction)
	{
		if (!StatusByReact.TryGetValue(reaction.Emote.ToString(), out ReactionStatus status))
			status = ReactionStatus.None;

		return status;
	}
	public static RpsSelection GetUserSelection(this ReactionStatus reactionStatus)
	{
		return reactionStatus switch
		{
			ReactionStatus.RpsRock => RpsSelection.Rock,
			ReactionStatus.RpsPaper => RpsSelection.Paper,
			ReactionStatus.RpsScissors => RpsSelection.Scissors,
			_ => throw new ArgumentException()
		};
	}

	public static bool IncrementRpsWins(this SocketGuildUser user)
	{
		if (user.Guild.Id != 390334803972587530) // TODO: make this not hardcoded lol (its the ID for the testing lab) -jolk 2022-02-15
		{
			// TODO: increment on the global leaderboard once i have that going -jolk 2022-02-15
		}

		return Guilds.IncrementRpsWins(user.Guild.Id, user.Id);
	}
	public static bool IncrementRpsLosses(this SocketGuildUser user)
	{// see comments on IncrementRpsWins
		if (user.Guild.Id != 390334803972587530) { }

		return Guilds.IncrementRpsLosses(user.Guild.Id, user.Id);
	}

	public static RpsWinner RpsLogic(this ChallengerOpponentPair<RpsSelection> selections)
	{
		if (selections.Challenger == selections.Opponent)
			return RpsWinner.Draw;

		return (RpsWinner)((((((byte)selections.Challenger << 3) | (byte)selections.Opponent) % 9) - 1) & 2);
	}
}