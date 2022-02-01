using Discord;

namespace Appalachia.Utility
{
	public static class Reactions
	{
		public static readonly IEmote
			RpsAccept = new Emoji("✅"),
			RpsDeny = new Emoji("❌"),

			RpsRock = new Emoji("✊"),
			RpsPaper = new Emoji("🖐"),
			RpsScissors = new Emoji("✌️");
	}
}
