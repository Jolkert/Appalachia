using Appalachia.Data;
using Appalachia.Extensions;
using Appalachia.Utility;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			Program.Logger.Info(Source, "Reloading json files...");

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

		[Command("shutdown"), Alias("stop"), Name(Source + "/Shutdown")]
		public Task Shutdown()
		{
			Program.Logger.Info(Source, "Shutting down bot...");
			Program.Stop();
			return Task.CompletedTask;
		}

		[Command("printserver"), Alias("servprint", "sprint", "spr"), Name(Source + "/PrintGuild")]
		public async Task PrintGuild(ulong guildId = 0)
		{
			SocketGuild guild;
			if (guildId == 0)
				guild = Context.Guild;
			else
				guild = Program.Client.GetGuild(guildId);

			if (guild == null)
			{
				await Context.Channel.SendMessageAsync("Could not find guild with matching ID!");
				return;
			}

			string output = $"Guild {guild.Id}\n" +
							$"Name: {guild.Name}\nChannels:\n";

			// Add Channels
			foreach (SocketCategoryChannel category in guild.CategoryChannels.OrderBy(cat => cat.Position))
			{
				// Voice and non-voice are separate to preserve ordering (position indexing is separate between voice and text channels) -jolk 2022-07-10
				output += $"\t- {category.Name} [{category.Position}]\n\t\t- ";

				// I apologize that this foreach is a complete disaster. It had to be done. I like the sugar too much -jolk 2022-07-10
				foreach (SocketGuildChannel channel in category.Channels.Where(ch => ch.GetChannelType() != ChannelType.Voice).OrderBy(ch => ch.Position))
					output += $"({channel.GetChannelType().Value.ToString()[0]}) {channel.GetNameWithId()}{(channel.IsAnnouncementChannel() ? " <announcements>" : "")}{(channel.IsQuoteChannel() ? " <quotes>" : "")}\n";

				foreach (SocketGuildChannel channel in category.Channels.Where(ch => ch.GetChannelType() == ChannelType.Voice).OrderBy(ch => ch.Position))
					output += $"(V) {channel.GetNameWithId()}\n";
			}

			// Add roles (excluding @everyone)
			output += $"Roles: ({guild.Roles.Count})\n";
			foreach (SocketRole role in guild.Roles.OrderByDescending(r => r.Position).Where(r => !r.IsEveryone))
				output += $"\t- {role.GetNameWithId()} <#{role.Color.RawValue:x6}>\n";

			// Add members
			output += $"Members: ({guild.MemberCount})\n";
			await foreach (IReadOnlyCollection<IGuildUser> subcontainer in guild.GetUsersAsync())
			{
				foreach (IGuildUser user in subcontainer)
				{
		 			output += $"\t- {user.GetFullUsername()} ({user.Id})\n";
					foreach (SocketRole role in user.RoleIds.Select(id => guild.GetRole(id)).OrderByDescending(r => r.Position).Where(r => !r.IsEveryone))
						output += $"\t\t- {role.Name}\n";
				}
			}

			output += $"Owner: {guild.Owner.GetFullUsername()}";

			string outputFolder = "Resources/out", outputFile = "server_info.agi";
			if (!Directory.Exists(outputFolder))
				Directory.CreateDirectory(outputFolder);

			File.WriteAllText($"{outputFolder}/{outputFile}", output);
		}
	}
}
