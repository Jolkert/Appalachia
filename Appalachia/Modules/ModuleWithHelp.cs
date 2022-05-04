using Appalachia.Utility;
using Appalachia.Utility.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	public abstract class ModuleWithHelp : ModuleBase<SocketCommandContext>, IModuleBase
	{
		// TODO: maybe at some point have this stuff autofill from the wiki? Could be a neat idea but could also not work too great idk -jolk 2022-05-04
		public abstract string ModuleName { get; }
		public abstract string Description { get; }
		public abstract string Usage { get; } // make sure to take the command name itself out of this thanks

		[Command("help"), Alias("?"), Priority(int.MaxValue)] // does this do anything? i doubt it; update: apparently it does! -jolk 2022-04-28
		public async Task HelpCommand()
		{
			(string mainAlias, EmbedBuilder embed) = EmbedHelper.GenerateHelpEmbed(this);
			string wikiLink = $"{HelpModule.WikiUrl}#{mainAlias.Split(' ')[^1]}".Replace(' ', '-');
			await Context.Channel.SendEmbedAsync(embed, new ButtonBuilder("Need more help? Check the wiki page!", null, ButtonStyle.Link, wikiLink, new Emoji("❓")));
		}
	}
}