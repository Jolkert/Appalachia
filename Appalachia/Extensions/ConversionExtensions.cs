using Appalachia.Utility;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Appalachia.Extensions;

public static class ConversionExtensions
{
	private static IReadOnlyDictionary<string, string> NamesAndUnicodesReversed { get; }
	static ConversionExtensions()
	{
		// private access modifier? what for? -jolk 2022-07-21
		IReadOnlyDictionary<string, string> namesAndUnicodes = (IReadOnlyDictionary<string, string>)
																(typeof(Emoji).GetProperty("NamesAndUnicodes", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null));

		Regex isNameRegex = new Regex(@":.+?:");
		Regex skinToneDescriptorRegex = new Regex(@"(light|medium|dark)_skin_tone", RegexOptions.ExplicitCapture);

		Dictionary<string, string> reversed = new Dictionary<string, string>();
		foreach (KeyValuePair<string, string> entry in namesAndUnicodes)
		{
			// use :thumbsup: and :thumbsdown: instead of :+1: and :-1:
			if (entry.Key.Contains(":+1") || entry.Key.Contains(":-1"))
				continue;

			// use :[name]_tone#: instead of :[name]_skin-tone-#: or :[name]_[descriptor]_skin_tone:
			MatchCollection matches = isNameRegex.Matches(entry.Key);
			if (matches.Count != 1 || skinToneDescriptorRegex.IsMatch(entry.Key))
				continue;

			if (!reversed.ContainsKey(entry.Value))
				reversed.Add(entry.Value, entry.Key);
		}

		NamesAndUnicodesReversed = reversed;
	}

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
		if (times <= 1)
			return str;

		switch (str.Length)
		{
			case 0:
				return "";
			case 1:
				return new string(str[0], times);
			default:
				StringBuilder builder = new StringBuilder(str.Length * times);
				for (int i = 0; i < times; i++)
					builder.Append(str);
				return builder.ToString();
		}
	}
	public static string Repeat(this char chr, int times)
	{
		return new string(chr, times);
	}

	public static string ToStringAllowNull(this object obj)
	{
		return obj != null ? obj.ToString() : string.Empty;
	}

	public static string ToDiscordName(this IEmote emote)
	{
		if (NamesAndUnicodesReversed.ContainsKey(emote.ToString()))
			return NamesAndUnicodesReversed[emote.ToString()];
		else
			return emote.ToString();
	}
}