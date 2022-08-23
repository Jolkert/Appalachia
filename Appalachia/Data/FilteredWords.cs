using System.Collections.Generic;

namespace Appalachia.Data
{
	public class FilteredWords : BaseJsonDataHolder<List<string>>
	{
		public FilteredWords(string fileName) : base(fileName, new List<string>()) { }

		public string[] GetFilteredWords()
		{
			return _data.ToArray();
		}
	}
}