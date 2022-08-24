using Appalachia.Extensions;
using Appalachia.Utility;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Appalachia.Modules;

public abstract class ModuleWithHelp : ModuleBase<SocketCommandContext>, IModuleBase
{
	// TODO: maybe at some point have this stuff autofill from the wiki? Could be a neat idea but could also not work too great idk -jolk 2022-05-04
	public abstract string ModuleName { get; }
	public abstract string Description { get; }
	public abstract string Usage { get; } // make sure to take the command name itself out of this thanks

	[Command("help"), Alias("?"), Priority(int.MaxValue)] // does this do anything? i doubt it; update: apparently it does! -jolk 2022-04-28
	public async Task HelpCommand()
	{
		ModuleInfo foundModule = Program.CommandHandler.Commands.Modules.Where(mod => mod.Name == ModuleName).FirstOrDefault();
		string mainAlias = foundModule?.Aliases[0];
		IEnumerable<string> aliases = foundModule?.Aliases.Select(cmdStr => cmdStr.Split(' ').Last()).Distinct();

		List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
		fields.Add(new EmbedFieldBuilder().WithName("Usage").WithValue($"`{Program.Config.Settings.CommandPrefix}{mainAlias} {Usage}`").WithIsInline(false));

		if (aliases != null && aliases.Count() > 1)
			fields.Add(new EmbedFieldBuilder().WithName("Aliases").WithValue(string.Join(", ", aliases.Where(str => str != mainAlias).Select(str => $"`{str}`"))).WithIsInline(false));

		EmbedBuilder embed = new EmbedBuilder().WithTitle($"{ModuleName} Help")
											   .WithDescription(Description)
											   .WithFields(fields)
											   .WithColor(Colors.Default)
											   .WithFooter(new EmbedFooterBuilder().WithText($"Need more help? Run {Program.Config.Settings.CommandPrefix}help")
																				   .WithIconUrl(Program.Client.CurrentUser.GetAvatarUrl()));
		// before you try to codeblock part of the footer again, markup doesnt work in footers -jolk 2022-01-11


		string wikiLink = $"{HelpModule.WikiUrl}/Commands#{mainAlias.Split(' ')[^1]}";
		await Context.Channel.SendEmbedAsync(embed, new ButtonBuilder("Need more help? Check the wiki page!", null, ButtonStyle.Link, wikiLink, new Emoji("\u2753")));
	}
}