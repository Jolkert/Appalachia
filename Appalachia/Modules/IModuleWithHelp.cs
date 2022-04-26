using System.Threading.Tasks;

namespace Appalachia.Modules
{
	public interface IModuleWithHelp
	{
		// TODO: getting help working on everything is gonna be a pain in the ass and im not sure how its gonna work for some of them -jolk 2022-01-18
		// TODO: completely rework the command naming scheme. no idea how exactly im gonna do that, but the current system is ultra garbage -jolk 2022-04-19

		public abstract string ModuleName { get; }
		public abstract Task HelpCommand();
	}
}