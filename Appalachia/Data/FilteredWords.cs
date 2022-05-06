using System.Collections.Generic;

namespace Appalachia.Data
{
	public class FilteredWords : BaseJsonDataHolder<List<string>>
	{
		private const string GolbalFilterFile = "filtered_words.json";

		public FilteredWords() : base(GolbalFilterFile, new List<string>()) { }

		public string[] GetFilteredWords()
		{
			return _data.ToArray();
		}
	}
}
