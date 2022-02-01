using Appalachia.Utility;
using System;
using System.Collections.Generic;

namespace Appalachia.Data
{
	public class ServerData : BaseJsonDataHolder<Dictionary<ulong, Server>>
	{
		// currently debating whether i should put leaderboards in ServerData or make it its own thing
		// making it its own thing *does* open up the possibility of global leaderboards. which could be interesting
		// either way its 4am. im going to sleep -jolk 2022-01-10
		private const string ServersFile = "servers.json";

		public ServerData() : base(ServersFile, new Dictionary<ulong, Server>()) { }

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

		public Server(ulong quoteChannelId = 0, ulong announcementChannelId = 0, uint color = Colors.Default)
		{
			this.QuoteChannelId = quoteChannelId;
			this.AnnouncementChannelId = announcementChannelId;
			this.Color = color;
		}
	}
}
