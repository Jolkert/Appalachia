using Newtonsoft.Json;
using System.IO;

namespace Appalachia.Data
{
	public abstract class BaseJsonDataHolder<T> : IJsonDataHolder
	{
		private const string DataFolder = "Resources/data";
		private readonly string _fileName;
		private static readonly JsonSerializerSettings _verboseSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

		protected T _data;

		public BaseJsonDataHolder(string fileName, T defaultData)
		{
			_fileName = $"{DataFolder}/{fileName}";

			if (!Directory.Exists(DataFolder))
				Directory.CreateDirectory(DataFolder);

			if (!File.Exists(_fileName))
			{
				_data = defaultData;
				WriteJson();
			}
			else
				ReloadJson();
		}

		protected void ReloadJson(bool verbose = false)
		{
			_data = JsonConvert.DeserializeObject<T>(File.ReadAllText(_fileName), verbose ? _verboseSettings : null);
			if (Program.Logger != null)
				Program.Logger.Info($"{GetType().Name} reloaded!");
		}
		protected void WriteJson(bool verbose = false)
		{
			File.WriteAllText(_fileName, JsonConvert.SerializeObject(_data, Formatting.Indented, verbose ? _verboseSettings : null));
		}

		public virtual void ReloadJson()
		{
			ReloadJson(false);
		}
		public virtual void WriteJson()
		{
			WriteJson(false);
		}
	}
}