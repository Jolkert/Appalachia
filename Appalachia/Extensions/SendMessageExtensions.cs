using Appalachia.Modules;
using Appalachia.Utility;
using Discord;
using System.Threading.Tasks;

namespace Appalachia.Extensions;

public static class SendMessageExtensions
{
	public static async Task<IMessage> SendEmbedAsync(this IMessageChannel channel, Embed embed, ButtonBuilder[] buttons)
	{
		ComponentBuilder component = null;
		if (buttons != null)
		{
			component = new ComponentBuilder();
			foreach (ButtonBuilder button in buttons)
				component = component.WithButton(button);
		}

		return await channel.SendMessageAsync("", false, embed, null, null, null, component?.Build());
	}
	public static async Task<IMessage> SendEmbedAsync(this IMessageChannel channel, EmbedBuilder embedBuilder, ButtonBuilder[] buttons = null)
	{
		return await SendEmbedAsync(channel, embedBuilder.Build(), buttons);
	}
	public static async Task<IMessage> SendEmbedAsync(this IMessageChannel channel, EmbedBuilder embedBuilder, ButtonBuilder button)
	{
		return await SendEmbedAsync(channel, embedBuilder, new ButtonBuilder[] { button });
	}
	public static async Task<IMessage> ReplyEmbedAsync(this IUserMessage message, Embed embed, bool mention = false)
	{
		if (!mention)
			return await message.ReplyAsync("", false, embed, AllowedMentions.None);
		else
			return await message.ReplyAsync("", false, embed, AllowedMentions.All);
	}
	public static async Task<IMessage> ReplyEmbedAsync(this IUserMessage message, EmbedBuilder embedBuilder, bool mention = false)
	{
		return await ReplyEmbedAsync(message, embedBuilder.Build(), mention);
	}

	private static readonly ButtonBuilder[] ErrorMessageButtons = new ButtonBuilder[]
	{
		new ButtonBuilder("Get more help", null, ButtonStyle.Link, HelpModule.WikiUrl, new Emoji("\u2753")),
		new ButtonBuilder("Report a bug", null, ButtonStyle.Link, HelpModule.IssueReportUrl, new Emoji("\uD83D\uDC1C"))
	};
	public static async Task<IMessage> SendErrorMessageAsync(this IMessageChannel channel, string errorMessage)
	{
		EmbedBuilder embed = new EmbedBuilder().WithTitle("Oops!")
											   .WithDescription($"*{errorMessage}*")
											   .WithColor(Colors.Error)
											   .WithFooter("Need to report a bug? Contact my creator at Jolkert#2991");
		return await SendEmbedAsync(channel, embed, ErrorMessageButtons);
	}
}