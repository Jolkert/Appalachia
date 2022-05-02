using System.Collections.Generic;

namespace Appalachia.Data
{
	public class FilteredWords : BaseJsonDataHolder<List<string>>
	{
		private const string BannedWordsFile = "filtered_words.json";

		public bool ShouldMatchSubstitutions { get; set; } // idk what else to call this. its whether or not to check for common fiter-avoidance characters -jolk 2022-05-02

		public FilteredWords(bool shouldMatchSubstitutions = true) : base(BannedWordsFile, new List<string>())
		{
			this.ShouldMatchSubstitutions = shouldMatchSubstitutions;
		}

		public string[] GetFilteredWords()
		{
			return _data.ToArray();
		}

		public override void ReloadJson()
		{
			base.ReloadJson();
			Program.LogAsync("Banned words reloaded!", GetType().Name);
		}
	}
}
