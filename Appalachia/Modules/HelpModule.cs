using Appalachia.Extensions;
using Appalachia.Utility;
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

		public const string WikiUrl = "https://github.com/Jolkert/Appalachia/wiki";
		public const string IssueReportUrl = "https://github.com/Jolkert/Appalachia/issues/new";
		private const string OAuthUrl = "https://discord.com/api/oauth2/authorize?client_id=519292417816395779&permissions=8&scope=bot%20applications.commands";
		private const ulong CreatorId = 227916147540885505; // this is just so i can refer to myself lmao. maybe move this to Program tho? -jolk 2022-04-28

		private static readonly EmbedBuilder embed = new EmbedBuilder().WithTitle("Appalachia Help")
												 .WithDescription($"Command prefix is `{Program.Config.Settings.CommandPrefix}`\n" +
																  $"For more information about a specific command, run `{Program.Config.Settings.CommandPrefix}help <command>`\n" +
																  $"For information about all Appalachia commands, see the wiki below")
												 .WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl())
												 .WithColor(Colors.Default)
												 .WithFooter(new EmbedFooterBuilder().WithText($"Bot author: Jolkert#2991 ・ v{Program.Version}")
																					 .WithIconUrl(Program.Client.GetUser(CreatorId).GetAvatarUrl()));

		[Command, Name(Source), Priority(1)]
		public async Task HelpCommand()
		{
			await Context.Channel.SendEmbedAsync(embed, new ButtonBuilder[]
			{
				new ButtonBuilder("Commands Wiki", null, ButtonStyle.Link, WikiUrl, new Emoji("\u2753")),
				new ButtonBuilder("Report a bug", null, ButtonStyle.Link, WikiUrl, new Emoji("\uD83D\uDC1C")),
				new ButtonBuilder("Add to Your Server!", null, ButtonStyle.Link, OAuthUrl, Emote.Parse("<:appalachia:969373293499007006>"))
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
