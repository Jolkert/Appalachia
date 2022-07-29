using Appalachia.Data;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Appalachia.Utility.Util;

namespace Appalachia.Extensions
{
	/* Extension methods for accessing and modifying guild data
	 * use these instead of Util.Guilds instance methods thanks
	 * -jolk 2022-07-08
	 */
	public static class GuildExtensions
	{
		// Accessors
		public static SocketTextChannel GetQuoteChannel(this SocketGuild guild)
		{
			return guild.GetTextChannel(Guilds.GetQuoteChannelId(guild.Id));
		}
		public static SocketTextChannel GetAnnouncementChannel(this SocketGuild guild)
		{
			return guild.GetTextChannel(Guilds.GetAnnouncementChannelId(guild.Id));
		}
		public static SocketRole GetDefaultRole(this SocketGuild guild)
		{
			ulong defaultRoleId = Guilds.GetDefaultRoleId(guild.Id);
			if (defaultRoleId == 0)
				return null;
			else
				return guild.GetRole(defaultRoleId);
		}
		public static uint GetColor(this SocketGuild guild)
		{
			return Guilds.GetColorOrDefault(guild.Id);
		}
		public static string[] GetFilteredWords(this SocketGuild guild)
		{
			return Guilds.GetFilteredWords(guild.Id);
		}
		public static (ulong announcements, ulong quotes) CheckForImportantChannels(this SocketGuild guild)
		{
			SocketTextChannel announcementChannel = null, quoteChannel = null;
			foreach (SocketTextChannel textChannel in guild.TextChannels)
			{
				if (announcementChannel == null && (textChannel.GetChannelType() == ChannelType.News || textChannel.Name.Contains("announcements")))
					announcementChannel = textChannel;

				if (quoteChannel == null && textChannel.Name.Contains("quote"))
					quoteChannel = textChannel;
			}

			return (announcementChannel?.Id ?? 0, quoteChannel?.Id ?? 0);
		}

		public static KeyValuePair<ulong, Guild.UserScore>[] GetRpsLeaderboard(this SocketGuild guild)
		{
			return Guilds.GetSortedRpsLeaderboard(guild.Id);
		}
		public static Guild.UserScore GetGuildRpsScore(this SocketGuildUser user)
		{
			return Guilds.GetUserScore(user.Guild.Id, user.Id);
		}
		public static int GetGuildRpsRank(this SocketGuildUser user)
		{
			return Guilds.GetUserRank(user.Guild.Id, user.Id);
		}

		// Modifiers
		public static GuildData.ModificationResult SetQuoteChannel(this SocketGuild guild, SocketTextChannel channel)
		{
			return Guilds.SetQuoteChannelId(guild.Id, channel?.Id ?? 0);
		}
		public static GuildData.ModificationResult SetAnnouncementChannel(this SocketGuild guild, SocketTextChannel channel)
		{
			return Guilds.SetAnnouncementChannelId(guild.Id, channel?.Id ?? 0);
		}
		public static GuildData.ModificationResult SetDefaultRole(this SocketGuild guild, SocketRole role)
		{
			return Guilds.SetDefaultRoleId(guild.Id, role?.Id ?? 0);
		}

		public static GuildData.ModificationResult SetColor(this SocketGuild guild, uint color)
		{
			return Guilds.SetColor(guild.Id, color);
		}
		public static GuildData.ModificationResult AddFilteredWords(this SocketGuild guild, params string[] words)
		{
			return Guilds.AddFilteredWords(guild.Id, words);
		}
		public static GuildData.ModificationResult RemoveFilteredWords(this SocketGuild guild, params string[] words)
		{
			return Guilds.RemoveFilteredWords(guild.Id, words);
		}
		public static GuildData.ModificationResult ClearFilteredWords(this SocketGuild guild)
		{
			return Guilds.ClearFilteredWords(guild.Id);
		}

		public static bool IsAnnouncementChannel(this SocketGuildChannel channel)
		{
			return channel.Id == Guilds.GetAnnouncementChannelId(channel.Guild.Id);
		}
		public static bool IsQuoteChannel(this SocketGuildChannel channel)
		{
			return channel.Id == Guilds.GetQuoteChannelId(channel.Guild.Id);
		}

		public static bool HasFilteredWord(this IMessage message)
		{
			string[] globalFilteredWords = Filter.GetFilteredWords();
			string[] guildFilteredWords = Array.Empty<string>();
			if (message.Channel is SocketGuildChannel channel)
				guildFilteredWords = channel.Guild.GetFilteredWords();

			if (globalFilteredWords.Length == 0 && guildFilteredWords.Length == 0)
				return false;

			// this might get a tad overzealous? idk i'll (hopefully) come back to it later or smth -jolk 2022-02-14
			string regexString = string.Join('|', globalFilteredWords.Concat(guildFilteredWords)).ToLowerInvariant()
								.Replace("i", "[i!1]")
								.Replace("e", "[e3]")
								.Replace("o", "[o0]")
								.Replace("a", "[a4@]")
								.Replace("l", "[l1]")
								.Replace("b", "[b8]")
								.Replace("s", "[s5]")
								.Replace("z", "[z2]");

			return Regex.IsMatch(message.Content.ToLowerInvariant(), regexString);
		}
	}
}