using Appalachia.Utility;
using Appalachia.Utility.Extensions;
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

		public const string WikiUrl = "https://github.com/Jolkert/Appalachia/wiki/Commands";
		private const string OAuthUrl = "https://github.com/Jolkert/Appalachia/wiki"; // remember to add this before release -jolk 2022-01-09
		private const ulong CreatorId = 227916147540885505; // this is just so i can refer to myself lmao. maybe move this to Program tho? -jolk 2022-04-28

		private static readonly EmbedBuilder embed = new EmbedBuilder().WithTitle("Appalachia Help")
												 .WithDescription($"Command prefix is `{Program.Config.Settings.CommandPrefix}`\n" +
																  $"For more information about a specific command, run `{Program.Config.Settings.CommandPrefix}help <command>`\n" +
																  $"For information about all Appalachia commands, see the wiki below")
												/* .WithFields(new EmbedFieldBuilder[]
												 {
													new EmbedFieldBuilder().WithName("Helpful links").WithValue($"[Command Help]({WikiUrl})\n[Add to your own server!]({OAuthUrl})")
												 })*/ // probaby remove this block at some point. i think the link buttons are quite a bit better -jolk 2022-04-28
												 .WithColor(Colors.Default)
												 .WithFooter(new EmbedFooterBuilder()
												 .WithIconUrl(Program.Client.GetUser(CreatorId).GetAvatarUrl())
												 .WithText($"Bot author: Jolkert#2991 | v{Program.Version}"));

		[Command, Name(Source), Priority(1)]
		public async Task HelpCommand()
		{
			await Context.Channel.SendEmbedAsync(embed, new ButtonBuilder[]
			{
				new ButtonBuilder("Command Help Wiki", null, ButtonStyle.Link, WikiUrl, new Emoji("❓")),
				new ButtonBuilder("Add to Your Own Server!", null, ButtonStyle.Link, OAuthUrl, Emote.Parse("<:appalachia:969373293499007006>"))
			});
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
