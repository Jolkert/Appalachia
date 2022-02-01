using Appalachia.Data;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Appalachia.Utility.Extensions
{
	public static class Extensions
	{
		private static readonly Dictionary<string, ReactionStatus> StatusByReact = new Dictionary<string, ReactionStatus>()
		{
			{Reactions.RpsAccept.Name, ReactionStatus.RpsAccept},
			{Reactions.RpsDeny.Name, ReactionStatus.RpsDeny},

			{Reactions.RpsRock.Name, ReactionStatus.RpsRock},
			{Reactions.RpsPaper.Name, ReactionStatus.RpsPaper},
			{Reactions.RpsScissors.Name, ReactionStatus.RpsScissors}
		};

		// these used to be in Util, but then i realized that I can just make them extension methods instead -jolk 2022-01-11
		public static string GetFullUsername(this IUser user)
		{
			return $"{user.Username}#{user.Discriminator}";
		}
		public static string GetGuildOrDefaultAvatarUrl(this IUser rawUser)
		{
			if (rawUser is SocketGuildUser guildUser)
				return guildUser?.GetGuildAvatarUrl() ?? guildUser.GetAvatarUrl();
			else
				return rawUser?.GetAvatarUrl();
		}

		public static string GetGuildChannelName(IGuild guild, IMessageChannel channel)
		{// i will not give into the temptation of ternary. i refuse -jolk 2022-01-06
			if (guild != null)
				return $"{guild.Name}/#{channel.Name}";
			else
				return channel.Name;
		}
		public static string GetGuildChannelName(this ICommandContext context)
		{
			return GetGuildChannelName(context.Guild, context.Channel);
		}
		public static string GetGuildChannelName(this IMessageChannel channel)
		{
			if (channel is SocketGuildChannel guildChannel)
				return GetGuildChannelName(guildChannel.Guild, channel);
			else
				return GetGuildChannelName(null, channel);
		}

		public static string GetNameWithId(this IGuild guild)
		{
			return $"{guild.Name}({guild.Id})";
		}
		public static string GetNameWithId(this IChannel channel)
		{
			return $"{channel.Name}({channel.Id})";
		}

		public static string ToColorString(this Color color, bool withRgb = true)
		{// i am sorry. i like oneliners too much. i really have to no -jolk 2022-01-06
			return $"{color.ToString().ToLowerInvariant()}{(withRgb ? $" ({color.R}, {color.G}, {color.B})" : "")}";
		}
		public static IEmote ToEmote(this RpsSelection selection)
		{
			return selection switch
			{
				RpsSelection.Rock => Reactions.RpsRock,
				RpsSelection.Paper => Reactions.RpsPaper,
				RpsSelection.Scissors => Reactions.RpsScissors,
				_ => null
			};
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
		public static RpsWinner RpsLogic(this ChallengerOpponentPair<RpsSelection> selections)
		{
			if (selections.Challenger == selections.Opponent)
				return RpsWinner.Draw;

			return (RpsWinner)((((((byte)selections.Challenger << 3) | (byte)selections.Opponent) % 9) - 1) & 2);
		}

		public static uint ParseGameId(this IEmbed embed)
		{
			string footer = embed.Footer?.Text;
			return footer != null && footer.Contains("Match ID: ") ? Convert.ToUInt32(footer[^6..], 16) : 0;
		}
		public static async Task AddRpsReactionsAsync(this IMessage message)
		{
			await message.AddReactionAsync(Reactions.RpsRock);
			await message.AddReactionAsync(Reactions.RpsPaper);
			await message.AddReactionAsync(Reactions.RpsScissors);
		}
	}
}
