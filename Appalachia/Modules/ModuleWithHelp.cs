using Discord.Commands;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	public abstract class ModuleWithHelp : ModuleBase<SocketCommandContext>, IModuleBase
	{
		// TODO: getting help working on everything is gonna be a pain in the ass and im not sure how its gonna work for some of them -jolk 2022-01-18
		// TODO: completely rework the command naming scheme. no idea how exactly im gonna do that, but the current system is ultra garbage -jolk 2022-04-19

		public abstract string ModuleName { get; }

		// not entirely certain if this is how im gonna do this, but i think it makes sense?
		public abstract string Description { get; }
		public abstract string Usage { get; }

		[Command("help"), Alias("?")] // does this do anything? i doubt it
		public abstract Task HelpCommand();
	}
}