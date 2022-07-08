using Appalachia.Utility;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;

namespace Appalachia.Extensions
{
	public static class ConversionExtensions
	{
		public static bool TryGetUser(this IGuild guild, string userArg, out SocketGuildUser user)
		{
			foreach (IGuildUser testUser in guild.GetUsersAsync().Result)
			{
				if ((testUser.Nickname ?? testUser.Username).ToLowerInvariant() == userArg.ToLowerInvariant() || testUser.GetFullUsername().ToLowerInvariant() == userArg.ToLowerInvariant())
				{
					user = testUser as SocketGuildUser; // whats the worst that can happen?? -jolk 2022-03-22
					return true;
				}
			}

			user = null;
			return false;
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

		public static string ToOrdinal(this int cardinal)
		{// is this readable? i mean its a bit wack, but it isnt too bad i guess. i'll leave it -jolk 2022-02-14
			return cardinal + (cardinal % 100) switch
			{
				11 or 12 or 13 => "th",
				_ => (cardinal % 10) switch
				{
					1 => "st",
					2 => "nd",
					3 => "rd",
					_ => "th",
				}
			};
		}
		public static string Repeat(this string str, int times)
		{
			return String.Concat(Enumerable.Repeat(str, times));
		}
	}
}
