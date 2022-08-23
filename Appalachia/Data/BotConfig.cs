using System;

namespace Appalachia.Data
{
	public class BotConfig : BaseJsonDataHolder<ConfigOptions>
	{
		public ConfigOptions Settings { get => _data; private set => _data = value; }

		public BotConfig(string fileName) : base(fileName, ConfigOptions.DefaultSettings) { }

		public void SetToken(string token)
		{
			Settings = new ConfigOptions(token, Settings.CommandPrefix, Settings.OutputLogsToFile);
			WriteJson();
		}

		public void SetPrefix(string prefix)
		{
			Settings = new ConfigOptions(Settings.Token, prefix, Settings.OutputLogsToFile);
			WriteJson();
		}
	}

	public struct ConfigOptions
	{
		// should i have a const for the default token string instead of just throwing a literal in here and also in Program.StartAsync()? maybe. idk.
		public static readonly ConfigOptions DefaultSettings = new ConfigOptions("BOT_TOKEN_GOES_HERE", "~", true);

		public string Token { get; set; }
		public string CommandPrefix { get; set; }
		public bool OutputLogsToFile { get; set; }

		public ConfigOptions(string token, string prefix, bool outputLogsToFile)
		{
			this.Token = token;
			this.CommandPrefix = prefix;
			this.OutputLogsToFile = outputLogsToFile;
		}
	}

	public class ConfigException : Exception
	{
		public string MissingFieldName { get; }

		public ConfigException(string message, string missingField) : base(message)
		{
			MissingFieldName = missingField;
		}
	}
}