using Discord;

namespace Appalachia.Utility;

public static class Reactions
{
	public static readonly IEmote
		RpsAccept = new Emoji("\u2705"),
		RpsDeny = new Emoji("\u274C"),

		RpsRock = new Emoji("\u270A"),
		RpsPaper = new Emoji("\uD83D\uDD90"),
		RpsScissors = new Emoji("\u270C");
}