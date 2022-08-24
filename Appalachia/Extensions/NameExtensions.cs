using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Appalachia.Extensions;

public static class NameExtensions
{
	public static string GetFullUsername(this IUser user)
	{
		return $"{user.Username}#{user.Discriminator}";
	}
	public static string GetGuildOrDefaultAvatarUrl(this IUser rawUser)
	{
		if (rawUser is SocketGuildUser guildUser)
			return guildUser?.GetGuildAvatarUrl() ?? guildUser.GetAvatarUrl();
		else
			return rawUser?.GetAvatarUrl();
	}

	public static string GetGuildChannelName(IGuild guild, IMessageChannel channel)
	{// i will not give into the temptation of ternary. i refuse -jolk 2022-01-06
		if (guild != null)
			return $"{guild.Name}/#{channel.Name}";
		else
			return channel.Name;
	}
	public static string GetGuildChannelName(this ICommandContext context)
	{
		return GetGuildChannelName(context.Guild, context.Channel);
	}
	public static string GetGuildChannelName(this IMessageChannel channel)
	{
		if (channel is SocketGuildChannel guildChannel)
			return GetGuildChannelName(guildChannel.Guild, channel);
		else
			return GetGuildChannelName(null, channel);
	}

	public static string GetNameWithId(this IGuild guild)
	{
		return $"{guild.Name}({guild.Id})";
	}
	public static string GetNameWithId(this IChannel channel)
	{
		return $"{channel.Name}({channel.Id})";
	}
	public static string GetNameWithId(this IRole role)
	{
		return $"{role.Name}({role.Id})";
	}
}