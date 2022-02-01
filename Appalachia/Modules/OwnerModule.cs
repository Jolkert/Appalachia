using Appalachia.Data;
using Appalachia.Utility;
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
				data.ReloadJson();

			await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
																.WithTitle("Reloaded!")
																.WithDescription("All json data was reloaded!")
																.WithColor(Colors.Default)
																.Build());
		}		

		[Command("shutdown"), Alias("stop"), Name(Source + "Shutdown")]
		public async Task Shutdown()
		{
			await Program.LogAsync("Shutting down bot...", Source);
			Program.Stop();
		}

		[Command("rpsclear"), Name(Source + "/RpsClear")]
		public async Task RpsClear()
		{
			await Task.Run(() => Util.Rps.ClearData());
		}

		[Command("quoteclear")]
		public async Task QuoteClear()
		{
			Util.Servers.SetQuoteChannelId(Context.Guild.Id, 0);
			await Context.Channel.SendMessageAsync("Quotes channel unset!");
		}
	}
}
