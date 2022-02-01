using System;

namespace Appalachia.Utility
{
	[Flags]
	public enum ReactionStatus : byte
	{
		None = 0,
		RpsAccept = 1,
		RpsDeny = 2,
		RpsConfirmations = RpsAccept | RpsDeny,

		RpsRock = 4,
		RpsPaper = 8,
		RpsScissors = 16,
		RpsSelections = RpsRock | RpsPaper | RpsScissors
	}
}
