using Appalachia.Data;
using System;

namespace Appalachia.Utility
{
	// so much of this class is just. a complete disaster. why did i write this like this? what am i even doing? -jolk 2022-01-09
	// TODO: i wanna rename this class but idk what to call it lol -jolk 2022-02-14
	public static class Util
	{
		// look i know this is bad. i know i probably *shouldnt* be doing this. but like come on. let me live. ive been writing code for literal hours to distract my brain. let me do this -jolk 2022-01-07
		public static readonly GuildData Guilds = new GuildData();
		public static readonly RpsData Rps = new RpsData();
		public static readonly FilteredWords Filter = new FilteredWords();
		public static readonly IJsonDataHolder[] DataHolders =
		{
			Program.Config,
			Guilds,
			Rps,
			Filter
		}; // again i know this is gross and probably bad practice or w/e but let me make my son in peace pls -jolk 2022-01-09

		public static readonly Random Rand = new Random();
	}
}