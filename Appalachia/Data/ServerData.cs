using Appalachia.Utility;
using Discord.WebSocket;
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

		// i did it. these are in Extensions now. still have to clean up parameter names tho -jolk 2022-02-15
		public void AddServer(ulong id, Server server)
		{
			if (!Exists(id))
			{
				_data.Add(id, server);
				WriteJson();
			}
		}
		public void AddServer(ulong id, ulong quoteChannelId = 0, ulong announcementChannelId = 0, uint color = Colors.Default)
		{
			AddServer(id, new Server(quoteChannelId, announcementChannelId, color));
		}
		public bool Exists(ulong id)
		{// Now that I added this to AddServer I dont think i need this but im gonna keep it anyways -jolk 2022-01-04
			return _data.ContainsKey(id);
		}

		public ulong GetQuoteChannelId(ulong? id)
		{
			Server server = _data.GetValueOrDefault(id ?? 0, null);
			return server != null ? server.QuoteChannelId : 0;
		}
		public ulong GetAnnouncementChannelId(ulong? id)
		{
			Server server = _data.GetValueOrDefault(id ?? 0, null);
			return server != null ? server.AnnouncementChannelId : 0;
		}
		public uint GetColorOrDefault(ulong? id)
		{
			Server server = _data.GetValueOrDefault(id ?? 0);
			return server != null ? server.Color : Colors.Default;
		}
		public Server.Score GetUserScore(ulong? guildId, ulong userId)
		{
			Server server = _data.GetValueOrDefault(guildId ?? 0, null);
			return server.RpsLeaderboard.GetValueOrDefault(userId);
		}
		public int GetUserRank(ulong? guildId, ulong userId)
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
		public IEnumerable<KeyValuePair<ulong, Server.Score>> GetSortedRpsLeaderboard(ulong? guildId)
		{
			Server server = _data.GetValueOrDefault(guildId ?? 0, null);
			return server?.RpsLeaderboard.ToArray()
							   .OrderByDescending(pair => pair.Value.Wins * Math.Round(pair.Value.WinRate, 4))
							   .ThenByDescending(pair => pair.Value.Wins)
							   .ThenBy(pair => pair.Value.Losses)
							   .ThenBy(pair => pair.Key);
		}

		public ModificationResult SetQuoteChannelId(ulong? id, ulong newQuoteChannelId)
		{
			if (!_data.TryGetValue(id ?? 0, out Server server))
				return ModificationResult.NotFound;

			if (server.QuoteChannelId == newQuoteChannelId)
				return ModificationResult.Unchanged;

			server.QuoteChannelId = newQuoteChannelId;
			WriteJson();
			return ModificationResult.Success;
		}
		public ModificationResult SetAnnouncementChannelId(ulong? id, ulong newAnnouncementChannelId)
		{
			if (!_data.TryGetValue(id ?? 0, out Server server))
				return ModificationResult.NotFound;

			if (server.AnnouncementChannelId == newAnnouncementChannelId)
				return ModificationResult.NotFound;

			server.AnnouncementChannelId = newAnnouncementChannelId;
			WriteJson();
			return ModificationResult.Success;
		}
		public ModificationResult SetColor(ulong? id, uint color)
		{
			if (!_data.TryGetValue(id ?? 0, out Server server))
				return ModificationResult.NotFound;

			if (server.Color == color)
				return ModificationResult.Unchanged;

			server.Color = color;
			WriteJson();
			return ModificationResult.Success;
		}
	
		public bool IncrementRpsWins(ulong serverId, ulong userId)
		{
			if (!_data.TryGetValue(serverId, out Server server))
				return false;
			if (!server.RpsLeaderboard.ContainsKey(userId))
				server.RpsLeaderboard.Add(userId, new Server.Score());

			server.RpsLeaderboard[userId].Wins++;
			WriteJson();
			return true;
		}
		public bool IncrementRpsLosses(ulong serverId, ulong userId)
		{
			if (!_data.TryGetValue(serverId, out Server server))
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

		// I really dont think this should be a dictionary. I kinda wanna make this like a normal list or smth that i sort on modification
		// that would make a lot more sense but would be a bit of effort to go refactor everything. idk prob eventually -jolk 2022-02-14
		// TODO: that ^
		public Dictionary<ulong, Score> RpsLeaderboard;

		public Server(ulong quoteChannelId = 0, ulong announcementChannelId = 0, uint color = Colors.Default)
		{
			this.QuoteChannelId = quoteChannelId;
			this.AnnouncementChannelId = announcementChannelId;
			this.Color = color;
			this.RpsLeaderboard = new Dictionary<ulong, Score>();
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
