using Appalachia.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Appalachia.Data
{
	public class ServerData : BaseJsonDataHolder<Dictionary<ulong, Server>>
	{
		// currently debating whether i should put leaderboards in ServerData or make it its own thing
		// making it its own thing *does* open up the possibility of global leaderboards. which could be interesting
		// either way its 4am. im going to sleep -jolk 2022-01-10
		private const string ServersFile = "servers.json";

		public ServerData() : base(ServersFile, new Dictionary<ulong, Server>()) { }

		// i did it. these are in Extensions now -jolk 2022-02-15
		// also cleaned up the methods in here. still like. dont use them outside of the extensions tho. thanks -jolk 2022-02-15
		public void AddServer(ulong guildId, Server server)
		{
			if (!Exists(guildId))
			{
				_data.Add(guildId, server);
				WriteJson();
			}
		}
		public void AddServer(ulong guildId, ulong quoteChannelId = 0, ulong announcementChannelId = 0, uint color = Colors.Default)
		{
			AddServer(guildId, new Server(quoteChannelId, announcementChannelId, color));
		}
		public bool Exists(ulong guildId)
		{// Now that I added this to AddServer I dont think i need this but im gonna keep it anyways -jolk 2022-01-04
			return _data.ContainsKey(guildId);
		}

		public ulong GetQuoteChannelId(ulong guildId)
		{
			return  _data.GetValueOrDefault(guildId)?.QuoteChannelId ?? 0;
		}
		public ulong GetAnnouncementChannelId(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.AnnouncementChannelId ?? 0;
		}
		public uint GetColorOrDefault(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.Color ?? Colors.Default;
		}
		public string[] GetFilteredWords(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.FilteredWords.ToArray();
		}

		public Server.Score GetUserScore(ulong guildId, ulong userId)
		{
			return _data.GetValueOrDefault(guildId)?.RpsLeaderboard.GetValueOrDefault(userId);
		}
		public int GetUserRank(ulong guildId, ulong userId)
		{
			int count = 1;
			foreach (var pair in GetSortedRpsLeaderboard(guildId))
			{
				if (pair.Key == userId)
					return count;
				count++;
			}

			return -1;
		}
		public IEnumerable<KeyValuePair<ulong, Server.Score>> GetSortedRpsLeaderboard(ulong guildId)
		{
			return _data.GetValueOrDefault(guildId)?.RpsLeaderboard.ToArray()
						.OrderByDescending(pair => pair.Value.Wins * Math.Round(pair.Value.WinRate, 4))
						.ThenByDescending(pair => pair.Value.Wins)
						.ThenBy(pair => pair.Value.Losses)
						.ThenBy(pair => pair.Key);
		}
		

		public ModificationResult SetQuoteChannelId(ulong guildId, ulong newQuoteChannelId)
		{
			if (!_data.TryGetValue(guildId, out Server server))
				return ModificationResult.NotFound;

			if (server.QuoteChannelId == newQuoteChannelId)
				return ModificationResult.Unchanged;

			server.QuoteChannelId = newQuoteChannelId;
			WriteJson();
			return ModificationResult.Success;
		}
		public ModificationResult SetAnnouncementChannelId(ulong guildId, ulong newAnnouncementChannelId)
		{
			if (!_data.TryGetValue(guildId, out Server server))
				return ModificationResult.NotFound;

			if (server.AnnouncementChannelId == newAnnouncementChannelId)
				return ModificationResult.NotFound;

			server.AnnouncementChannelId = newAnnouncementChannelId;
			WriteJson();
			return ModificationResult.Success;
		}
		public ModificationResult SetColor(ulong guildId, uint color)
		{
			if (!_data.TryGetValue(guildId, out Server server))
				return ModificationResult.NotFound;

			if (server.Color == color)
				return ModificationResult.Unchanged;

			server.Color = color;
			WriteJson();
			return ModificationResult.Success;
		}

		public ModificationResult AddFilteredWords(ulong guildId, params string[] words)
		{
			Server server = _data.GetValueOrDefault(guildId);
			if (server == null)
				return ModificationResult.NotFound;

			int count = 0;
			foreach (string word in words.Select(str => str.ToLowerInvariant()))
			{
				if (!server.FilteredWords.Contains(word) && word != "")
				{
					server.FilteredWords.Add(word);
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
			Server server = _data.GetValueOrDefault(guildId);
			if (server == null)
				return ModificationResult.NotFound;

			if (server.FilteredWords.RemoveAll(str => words.Contains(str.ToLowerInvariant())) > 0)
			{
				WriteJson();
				return ModificationResult.Success;
			}
			else
				return ModificationResult.Unchanged;
		}
		public ModificationResult ClearFilteredWords(ulong guildId)
		{
			Server server = _data.GetValueOrDefault(guildId);
			if (server == null)
				return ModificationResult.NotFound;

			if (server.FilteredWords.Count == 0)
				return ModificationResult.Unchanged;

			server.FilteredWords.Clear();
			WriteJson();
			return ModificationResult.Success;
		}

		public bool IncrementRpsWins(ulong guildId, ulong userId)
		{
			if (!_data.TryGetValue(guildId, out Server server))
				return false;
			if (!server.RpsLeaderboard.ContainsKey(userId))
				server.RpsLeaderboard.Add(userId, new Server.Score());

			server.RpsLeaderboard[userId].Wins++;
			WriteJson();
			return true;
		}
		public bool IncrementRpsLosses(ulong guildId, ulong userId)
		{
			if (!_data.TryGetValue(guildId, out Server server))
				return false;
			if (!server.RpsLeaderboard.ContainsKey(userId))
				server.RpsLeaderboard.Add(userId, new Server.Score());

			server.RpsLeaderboard[userId].Losses++;
			WriteJson();
			return true;
		}

		// holy fuck its like 5am. i am tired. i need to sleep -jolk 2022-01-03
		public override void ReloadJson()
		{
			base.ReloadJson();
			Program.LogAsync("Server data reloaded!", GetType().Name);
		}

		[Flags]
		public enum ModificationResult
		{
			Success = 1,
			Unchanged = 2,
			NotFound = 4
		}
	}

	public class Server
	{
		public ulong QuoteChannelId { get; set; }
		public ulong AnnouncementChannelId { get; set; }
		public uint Color { get; set; }
		public List<string> FilteredWords { get; set; }

		// I really dont think this should be a dictionary. I kinda wanna make this like a normal list or smth that i sort on modification
		// that would make a lot more sense but would be a bit of effort to go refactor everything. idk prob eventually -jolk 2022-02-14
		// TODO: that ^
		public Dictionary<ulong, Score> RpsLeaderboard { get; set; } // not sure why that wasnt a property before. knew something looked off. oops -jolk 2022-02-15

		public Server(ulong quoteChannelId = 0, ulong announcementChannelId = 0, uint color = Colors.Default)
		{
			this.QuoteChannelId = quoteChannelId;
			this.AnnouncementChannelId = announcementChannelId;
			this.Color = color;
			this.RpsLeaderboard = new Dictionary<ulong, Score>();
			this.FilteredWords = new List<string>();
		}

		public class Score
		{
			public int Wins { get; set; }
			public int Losses { get; set; }
			public double WinRate { get => (Wins + Losses == 0) ? 0 : (double)Wins / (Losses + Wins); }


			public Score()
			{
				this.Wins = 0;
				this.Losses = 0;
			}
			public Score(int wins, int losses)
			{
				this.Wins = wins;
				this.Losses = losses;
			}

			public override string ToString()
			{
				return $"{Wins}/{Losses}/{Math.Round(WinRate, 4)}";
			}
		}
	}
}
