using Appalachia.Utility;
using Appalachia.Utility.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	public abstract class ModuleWithHelp : ModuleBase<SocketCommandContext>, IModuleBase
	{
		// TODO: getting help working on everything is gonna be a pain in the ass and im not sure how its gonna work for some of them -jolk 2022-01-18
		// TODO: completely rework the command naming scheme. no idea how exactly im gonna do that, but the current system is ultra garbage -jolk 2022-04-19

		public abstract string ModuleName { get; }
		public abstract string Description { get; }
		public abstract string Usage { get; } // make sure to take the command name itself out of this thanks

		[Command("help"), Alias("?"), Priority(int.MaxValue)] // does this do anything? i doubt it; update: apparently it does! -jolk 2022-04-28
		public async Task HelpCommand()
		{
			await Program.LogAsync($"Getting help command from {this.GetType().Name}", ModuleName, LogSeverity.Debug);
			(string mainAlias, EmbedBuilder embed) = EmbedHelper.GenerateHelpEmbed(this);
			await Context.Channel.SendEmbedAsync(embed, new ButtonBuilder("Need more help?", null, ButtonStyle.Link, $"{HelpModule.WikiUrl}#{mainAlias}", new Emoji("❓")));
		}
	}
}