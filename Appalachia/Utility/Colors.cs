namespace Appalachia.Utility
{
	public static class Colors
	{
		// these technically like. *shouldnt* be consts? but idc. theyre consts dammit -jolk 2022-01-07
		// they're only consts so I can use default in method signatures lol -jolk 2022-01-12
		public const uint
			Default = 0xffff8d,
			Error = 0x900000,

			// Roll colors
			MinRoll = 0xff4c4c,
			BadRoll = 0xcc8400,
			AvgRoll = 0xffff4c,
			GoodRoll = 0x4ca64c,
			MaxRoll = 0x4cffff;
	}
}
