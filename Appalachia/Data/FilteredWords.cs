using System.Collections.Generic;

namespace Appalachia.Data
{
	public class FilteredWords : BaseJsonDataHolder<List<string>>
	{
		private const string BannedWordsFile = "filtered_words.json";
		public FilteredWords() : base(BannedWordsFile, new List<string>()) { }

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
