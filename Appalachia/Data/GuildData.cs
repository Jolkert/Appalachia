using Appalachia.Utility;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Appalachia.Data
{
	public class GuildData : BaseJsonDataHolder<Dictionary<ulong, Guild>>
	{
		// currently debating whether i should put leaderboards in GuildData or make it its own thing
		// making it its own thing *does* open up the possibility of global leaderboards. which could be interesting
		// either way its 4am. im going to sleep -jolk 2022-01-10
		private const string GuildsFile = "guilds.json";

		public GuildData() : base(GuildsFile, new Dictionary<ulong, Guild>()) { }

		// i did it. these are in Extensions now -jolk 2022-02-15
		// also cleaned up the methods in here. still like. dont use them outside of the extensions tho. thanks -jolk 2022-02-15
		public void AddGuild(ulong guildId, Guild guild)
		{
			if (!Exists(guildId))
			{
				_data.Add(guildId, guild);
				WriteJson();
			}
		}
		public void AddGuild(ulong guildId, ulong announcementChannelId = 0, ulong quoteChannelId = 0, uint color = Colors.Default)
		{
			AddGuild(guildId, new Guild(announcementChannelId, quoteChannelId, color));
		}
		public void RemoveGuild(ulong guildId)
		{
			if (Exists(guildId))
			{
				_data.Remove(guildId);
				WriteJson();
			}
		}
		public int RemoveMissingIds(params ulong[] guildIds)
		{
			int guildsRemoved = 0;
			foreach (KeyValuePair<ulong, Guild> pair in _data)
			{
				if (!guildIds.Contains(pair.Key))
				{
					RemoveGuild(pair.Key);
					guildsRemoved++;
				}
			}

			return guildsRemoved;
		}
		public bool Exists(ulong guildId)
		{// Now that I added this to AddGuild I dont think i need this but im gonna keep it anyways -jolk 2022-01-04
			return _data.ContainsKey(guildId);
		}

		public ulong GetQuoteChannelId(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.QuoteChannelId ?? 0;
		}
		public ulong GetAnnouncementChannelId(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.AnnouncementChannelId ?? 0;
		}
		public ulong GetDefaultRoleId(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.DefaultRoleId ?? 0;
		}
		public uint GetColorOrDefault(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.Color ?? Colors.Default;
		}
		public string[] GetFilteredWords(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.FilteredWords.ToArray();
		}

		public Guild.UserScore GetUserScore(ulong guildId, ulong userId)
		{
			return _data.GetValueOrDefault(guildId)?.RpsLeaderboard.GetValueOrDefault(userId);
		}
		public int GetUserRank(ulong guildId, ulong userId)
		{
			KeyValuePair<ulong, Guild.UserScore>[] leaderboard = GetSortedRpsLeaderboard(guildId);

			int rank = 1;
			for (int i = 0; i < leaderboard.Length; i++)
			{
				KeyValuePair<ulong, Guild.UserScore> pair = leaderboard[i];

				if (i > 0 && !pair.Equals(leaderboard[i - 1]))
					rank = i + 1;

				if (pair.Key == userId)
					return rank;
			}

			return -1;
		}
		public KeyValuePair<ulong, Guild.UserScore>[] GetSortedRpsLeaderboard(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.RpsLeaderboard.ToArray()
						.OrderByDescending(pair => pair.Value.Elo)
						.ThenByDescending(pair => pair.Value.WinRate)
						.ThenByDescending(pair => pair.Value.Wins)
						.ThenBy(pair => pair.Value.Losses)
						.ThenBy(pair => pair.Key).ToArray();
		}


		public ModificationResult SetQuoteChannelId(ulong guildId, ulong newQuoteChannelId)
		{
			if (!_data.TryGetValue(guildId, out Guild guild))
				return ModificationResult.NotFound;

			if (guild.QuoteChannelId == newQuoteChannelId)
				return ModificationResult.Unchanged;

			guild.QuoteChannelId = newQuoteChannelId;
			WriteJson();
			return ModificationResult.Success;
		}
		public ModificationResult SetAnnouncementChannelId(ulong guildId, ulong newAnnouncementChannelId)
		{
			if (!_data.TryGetValue(guildId, out Guild guild))
				return ModificationResult.NotFound;

			if (guild.AnnouncementChannelId == newAnnouncementChannelId)
				return ModificationResult.Unchanged;

			guild.AnnouncementChannelId = newAnnouncementChannelId;
			WriteJson();
			return ModificationResult.Success;
		}
		public ModificationResult SetDefaultRoleId(ulong guildId, ulong newDefaultRoleId)
		{
			if (!_data.TryGetValue(guildId, out Guild guild))
				return ModificationResult.NotFound;

			if (guild.DefaultRoleId == newDefaultRoleId)
				return ModificationResult.Unchanged;

			guild.DefaultRoleId = newDefaultRoleId;
			WriteJson();
			return ModificationResult.Success;
		}
		public ModificationResult SetColor(ulong guildId, uint color)
		{
			if (!_data.TryGetValue(guildId, out Guild guild))
				return ModificationResult.NotFound;

			if (guild.Color == color)
				return ModificationResult.Unchanged;

			guild.Color = color;
			WriteJson();
			return ModificationResult.Success;
		}

		public ModificationResult AddFilteredWords(ulong guildId, params string[] words)
		{
			Guild guild = _data.GetValueOrDefault(guildId);
			if (guild == null)
				return ModificationResult.NotFound;

			int count = 0;
			foreach (string word in words.Select(str => str.ToLowerInvariant()))
			{
				if (!guild.FilteredWords.Contains(word) && word != "")
				{
					guild.FilteredWords.Add(word);
					count++;
				}
			}

			if (count > 0)
			{
				WriteJson();
				return ModificationResult.Success;
			}
			else
				return ModificationResult.Unchanged;
		}
		public ModificationResult RemoveFilteredWords(ulong guildId, params string[] words)
		{
			Guild guild = _data.GetValueOrDefault(guildId);
			if (guild == null)
				return ModificationResult.NotFound;

			if (guild.FilteredWords.RemoveAll(str => words.Contains(str.ToLowerInvariant())) > 0)
			{
				WriteJson();
				return ModificationResult.Success;
			}
			else
				return ModificationResult.Unchanged;
		}
		public ModificationResult ClearFilteredWords(ulong guildId)
		{
			Guild guild = _data.GetValueOrDefault(guildId);
			if (guild == null)
				return ModificationResult.NotFound;

			if (guild.FilteredWords.Count == 0)
				return ModificationResult.Unchanged;

			guild.FilteredWords.Clear();
			WriteJson();
			return ModificationResult.Success;
		}

		public bool IncrementRpsWins(ulong guildId, ulong userId)
		{
			if (!_data.TryGetValue(guildId, out Guild guild))
				return false;
			if (!guild.RpsLeaderboard.ContainsKey(userId))
				guild.RpsLeaderboard.Add(userId, new Guild.UserScore());

			guild.RpsLeaderboard[userId].Wins++;
			WriteJson();
			return true;
		}
		public bool IncrementRpsLosses(ulong guildId, ulong userId)
		{
			if (!_data.TryGetValue(guildId, out Guild guild))
				return false;
			if (!guild.RpsLeaderboard.ContainsKey(userId))
				guild.RpsLeaderboard.Add(userId, new Guild.UserScore());

			guild.RpsLeaderboard[userId].Losses++;
			WriteJson();
			return true;
		}

		public (int, int) UpdateElo(SocketGuildUser winner, SocketGuildUser loser)
		{
			return UpdateElo(winner.GetGuildRpsScore(), loser.GetGuildRpsScore());
		}
		public (int, int) UpdateElo(Guild.UserScore p1Score, Guild.UserScore p2Score)
		{
			(int, int) previousElo = (p1Score.Elo, p2Score.Elo);

			p1Score.UpdateElo(true, p2Score);
			p2Score.UpdateElo(false, p1Score);

			WriteJson();

			return previousElo;
		}

		// holy fuck its like 5am. i am tired. i need to sleep -jolk 2022-01-03

		[Flags]
		public enum ModificationResult
		{
			Success = 1,
			Unchanged = 2,
			NotFound = 4
		}
	}

	public class Guild
	{
		public ulong AnnouncementChannelId { get; set; }
		public ulong QuoteChannelId { get; set; }
		public ulong DefaultRoleId { get; set; }
		public uint Color { get; set; }
		public List<string> FilteredWords { get; set; }
		public bool ShouldMatchSubstitutions { get; set; } // idk what else to call this. its whether or not to check for common fiter-avoidance characters -jolk 2022-05-02
														   // oops this should be in here lol -jolk 2022-05-05

		// I really dont think this should be a dictionary. I kinda wanna make this like a normal list or smth that i sort on modification
		// that would make a lot more sense but would be a bit of effort to go refactor everything. idk prob eventually -jolk 2022-02-14
		// TODO: that ^
		public Dictionary<ulong, UserScore> RpsLeaderboard { get; set; } // not sure why that wasnt a property before. knew something looked off. oops -jolk 2022-02-15

		public Guild(ulong announcementChannelId = 0, ulong quoteChannelId = 0, uint color = Colors.Default)
		{
			this.AnnouncementChannelId = announcementChannelId;
			this.QuoteChannelId = quoteChannelId;
			this.Color = color;
			this.RpsLeaderboard = new Dictionary<ulong, UserScore>();
			this.FilteredWords = new List<string>();
		}

		public class UserScore
		{
			public int Elo { get; set; }
			public int Wins { get; set; }
			public int Losses { get; set; }
			public double WinRate { get => (Wins + Losses == 0) ? 0 : (double)Wins / (Losses + Wins); }

			private static readonly double EloSensitivity = 40.0d; // this controls how much elo changes after a match. bigger number == bigger change -jolk 2022-03-22

			public UserScore()
			{
				this.Wins = 0;
				this.Losses = 0;
				this.Elo = 1500;
			}
			public UserScore(int wins, int losses, int elo = 1500)
			{
				this.Wins = wins;
				this.Losses = losses;
				this.Elo = elo;
			}

			public void UpdateElo(bool win, UserScore opponent)
			{
				UpdateElo(win, opponent.Elo);
			}

			public void UpdateElo(bool win, int opponentElo)
			{
				this.Elo += (int)(EloSensitivity * ((win ? 1 : 0) - (1.0 / (1 + Math.Pow(10, (opponentElo - this.Elo) / 400)))));
			}

			public override string ToString()
			{
				return $"{Elo}/{Wins}/{Losses}/{Math.Round(WinRate, 4)}";
			}
			public bool Equals(UserScore score)
			{
				return this.Wins == score.Wins && this.Losses == score.Losses;
			}
		}
	}
}
