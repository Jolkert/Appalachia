using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Appalachia.Data
{
	public class FilteredWords : BaseJsonDataHolder<List<string>>
	{
		private const string BannedWordsFile = "filtered_words.json";
		public FilteredWords() : base(BannedWordsFile, new List<string>()) { }

		public bool HasFilteredWord(string input)
		{
			if (_data.Count == 0)
				return false;

			// this might get a tad overzealous? idk i'll (hopefully) come back to it later or smth -jolk 2022-02-14
			string regex = string.Join('|', _data).ToLowerInvariant()
												  .Replace("i", "[i!1]")
												  .Replace("e", "[e3]")
												  .Replace("o", "[o0]")
												  .Replace("a", "[a4@]")
												  .Replace("l", "[l1]")
												  .Replace("b", "[b8]")
												  .Replace("s", "[s5]")
												  .Replace("z", "[z2]");

			return Regex.IsMatch(input.ToLowerInvariant(), regex);
		}

		public override void ReloadJson()
		{
			base.ReloadJson();
			Program.LogAsync("Banned words reloaded!", GetType().Name);
		}
	}
}
