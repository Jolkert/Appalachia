namespace Appalachia.Data
{
	public class BotConfig : BaseJsonDataHolder<ConfigOptions>
	{
		private const string ConfigFile = "config.json";
		public ConfigOptions Settings { get => _data; }

		public BotConfig() : base(ConfigFile, ConfigOptions.DefaultSettings) { }

		public void SetToken(string token)
		{
			Settings.SetToken(token);
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

		public void SetToken(string token)
		{
			this.Token = token;
		}
	}
}
