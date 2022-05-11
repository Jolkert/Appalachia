using Appalachia.Data;
using Appalachia.Utility;
using Appalachia.Utility.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	[Group, Name(Source), RequireOwner]
	public class OwnerModule : ModuleBase<SocketCommandContext>, IModuleBase
	{
		public const string Source = "Owner";

		[Command("reload"), Name(Source + "/Reload")]
		public async Task Reload()
		{
			await Program.LogAsync("Reloading json files...", Source);

			foreach (IJsonDataHolder data in Util.DataHolders)
			{
				data.ReloadJson();
				data.WriteJson();
			}

			await Context.Channel.SendEmbedAsync(new EmbedBuilder()
													.WithTitle("Reloaded!")
													.WithDescription("All json data was reloaded!")
													.WithColor(Colors.Default));
		}

		[Command("shutdown"), Alias("stop"), Name(Source + "Shutdown")]
		public async Task Shutdown()
		{
			await Program.LogAsync("Shutting down bot...", Source);
			Program.Stop();
		}
	}
}
