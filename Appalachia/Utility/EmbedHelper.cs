using Appalachia.Modules;
using Appalachia.Services;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Appalachia.Utility
{
	public static class EmbedHelper
	{
		public static EmbedBuilder GenerateErrorEmbed(string description)
		{
			return new EmbedBuilder()
				.WithTitle("Oops!")
				.WithDescription($"*{description}*")
				.WithColor(Colors.Error)
				.WithFooter("Need to report a bug? Contact my creator at Jolkert#2991");
		}
		public static EmbedBuilder GenerateHelpEmbed(string description, string usage, IModuleWithHelp module)
		{
			ModuleInfo foundModule = CommandHandler.Commands.Modules.Where(mod => mod.Name == module.ModuleName).FirstOrDefault();
			string mainAlias = foundModule?.Aliases[0];
			IEnumerable<string> aliases = foundModule?.Aliases.Select(cmdStr => cmdStr.Split(' ').Last()).Distinct();


			return new EmbedBuilder().WithTitle($"{module.ModuleName} Help")
					  .WithDescription(description)
					  .WithFields(new EmbedFieldBuilder[]
					  {
						  new EmbedFieldBuilder().WithName("Usage").WithValue($"`{Program.Config.Settings.CommandPrefix}{mainAlias} {usage}`").WithIsInline(false),
						  new EmbedFieldBuilder().WithName("Aliases").WithValue(string.Join(", ", aliases.Select(str => $"`{str}`"))).WithIsInline(false),
					  })
					  .WithColor(Colors.Default)
					  .WithFooter(new EmbedFooterBuilder().WithText($"Need more help? Run {Program.Config.Settings.CommandPrefix}help").WithIconUrl(Program.Client.CurrentUser.GetAvatarUrl()));
			// before you try to codeblock part of the footer again, markup doesnt work in footers -jolk 2022-01-11
		}
	}
}
