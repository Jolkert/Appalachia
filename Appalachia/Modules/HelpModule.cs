using Appalachia.Utility;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	[Group("help"), Alias("?"), Name(Source)]
	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private const string Source = "Help";

		private const string HelpDocUrl = "https://docs.google.com/document/d/190TFz_HNHp1lt4cnq1RdkJGXbYn4cLmO2dS0cXnCT00/edit?usp=sharing";
		private const string OAuthUrl = ""; // remember to add this before release -jolk 2022-01-09
		private const ulong CreatorId = 227916147540885505;

		private static readonly Embed embed = new EmbedBuilder().WithTitle("Appalachia Help")
												 .WithDescription($"Command prefix is `{Program.Config.Settings.CommandPrefix}`\n" +
																  $"For more information about a specific command, run `{Program.Config.Settings.CommandPrefix}help <command>`")
												 .WithFields(new EmbedFieldBuilder[]
												 {
													new EmbedFieldBuilder().WithName("Helpful links").WithValue($"[Command Help]({HelpDocUrl})\n[Add to your own server!]({OAuthUrl})")
												 })
												 .WithColor(Colors.Default)
												 .WithFooter(new EmbedFooterBuilder()
												 .WithIconUrl(Program.Client.GetUser(CreatorId).GetAvatarUrl())
												 .WithText($"Bot author: Jolkert#2991 | v{Program.Version}"))
												 .Build();

		[Command, Name(Source), Priority(1)]
		public async Task HelpCommand()
		{
			await Context.Channel.SendMessageAsync("", false, embed);
		}

		[Command, Name(Source)]
		public async Task HelpCommand([Remainder] string command)
		{
			command = command.ToLowerInvariant();

			ModuleInfo module = Services.CommandHandler.Commands.Modules.Where(mod => mod.Aliases.Contains(command)).FirstOrDefault();
			CommandInfo helpCommand = module?.Commands.Where(cmd => cmd.Aliases.Contains($"{command} help")).FirstOrDefault();

			if (helpCommand == null)
				await HelpCommand();
			else
				await helpCommand.ExecuteAsync(Context, new List<object>(), new List<object>(), null);
		}
	}
}
