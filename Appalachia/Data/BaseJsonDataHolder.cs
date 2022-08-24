using Newtonsoft.Json;
using System.IO;

namespace Appalachia.Data;

public abstract class BaseJsonDataHolder<T> : IJsonDataHolder
{
	private const string DataFolder = "Resources/data";
	private readonly string _fileName;
	private static readonly JsonSerializerSettings TypeSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

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

	public virtual void ReloadJson()
	{
		_data = JsonConvert.DeserializeObject<T>(File.ReadAllText(_fileName), TypeSettings);
		Program.Logger?.Info($"{GetType().Name} reloaded!");
	}
	public virtual void WriteJson()
	{
		File.WriteAllText(_fileName, JsonConvert.SerializeObject(_data, Formatting.Indented, TypeSettings));
	}
}