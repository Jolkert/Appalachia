using Appalachia.Utility;
using System.Collections.Generic;

namespace Appalachia.Data
{
	public class RpsData : BaseJsonDataHolder<Dictionary<DataKey, Dictionary<ulong, RpsChallenge>>>
	{
		private const string FileName = "rps.json";

		private Dictionary<ulong, RpsChallenge> Challenges
		{
			get => _data[DataKey.Challenges];
			set => _data[DataKey.Challenges] = value;
		}
		private Dictionary<ulong, RpsChallenge> ActiveGames
		{
			get => _data[DataKey.ActiveGames];
			set => _data[DataKey.ActiveGames] = value;
		}

		public RpsData() : base(FileName, new Dictionary<DataKey, Dictionary<ulong, RpsChallenge>>(){
			{ DataKey.Challenges, new Dictionary<ulong, RpsChallenge>() },
			{ DataKey.ActiveGames, new Dictionary<ulong, RpsChallenge>() }
		})
		{ }

		public RpsGame GetActiveGame(uint id)
		{
			return (RpsGame)ActiveGames.GetValueOrDefault(id);
		}
		public RpsChallenge GetChallenge(ulong messageId)
		{
			return Challenges.GetValueOrDefault(messageId);
		}

		public bool GameExists(uint id)
		{
			return ActiveGames.ContainsKey(id);
		}

		public void AddGame(ulong id, RpsGame rawGame)
		{

			ActiveGames.Add(id, rawGame);
			WriteJson();
		}
		public void AddChallenge(ulong messageId, RpsChallenge challenge)
		{
			Challenges.Add(messageId, challenge);
			WriteJson();
		}

		public bool RemoveGame(uint id)
		{
			bool success = ActiveGames.Remove(id);
			WriteJson();
			return success;
		}
		public bool RemoveChallenge(ulong messageId)
		{
			bool success = Challenges.Remove(messageId);
			WriteJson();
			return success;
		}

		public uint IncrementChallengerScore(uint gameId)
		{
			RpsGame game = GetActiveGame(gameId);
			game.Scores.Challenger++;
			WriteJson();
			return game.Scores.Challenger;
		}
		public uint IncrementOpponentScore(uint gameId)
		{
			RpsGame game = GetActiveGame(gameId);
			game.Scores.Opponent++;
			WriteJson();
			return game.Scores.Opponent;
		}
		public void IncrementRound(uint gameId)
		{
			GetActiveGame(gameId).IncrementRound();
			WriteJson();
		}

		public void SetChallengerSelection(uint gameId, RpsSelection selection)
		{
			GetActiveGame(gameId).Selections.Challenger = selection;
			WriteJson();
		}
		public void SetOpponentSelection(uint gameId, RpsSelection selection)
		{
			GetActiveGame(gameId).Selections.Opponent = selection;
			WriteJson();
		}
		public void ResetSelections(uint gameId, bool isBotMatch = false)
		{
			GetActiveGame(gameId).ClearSelections(isBotMatch);
			WriteJson();
		}

		public override void ReloadJson()
		{
			base.ReloadJson(true);
			Program.LogAsync("Rps data reloaded!", GetType().Name);
		}
		public override void WriteJson()
		{
			base.WriteJson(true);
		}

		// TODO: get rid of this later. i just need it for testing -jolk 2022-01-09
		public void ClearData()
		{
			this._data = new Dictionary<DataKey, Dictionary<ulong, RpsChallenge>>()
			{
				{ DataKey.Challenges, new Dictionary<ulong, RpsChallenge>() },
				{ DataKey.ActiveGames, new Dictionary<ulong, RpsChallenge>() }
			};
			WriteJson();
		}	
	}

	public class RpsChallenge
	{
		public GuildChannel GuildChannelIds { get; set; }
		public ChallengerOpponentPair<ulong> PlayerIds { get; set; }
		public uint FirstToScore { get; set; }

		public RpsChallenge() { }

		public RpsChallenge(ulong guildId, ulong channelId, ulong challengerId, ulong opponentId, uint firstToScore)
		{
			this.GuildChannelIds = new GuildChannel(guildId, channelId);
			this.PlayerIds = new ChallengerOpponentPair<ulong>(challengerId, opponentId);
			this.FirstToScore = firstToScore;
		}
	}
	public class RpsGame : RpsChallenge
	{
		public ChallengerOpponentPair<RpsSelection> Selections { get; set; }
		public ChallengerOpponentPair<uint> Scores { get; set; }
		public uint RoundCount { get; set; }
		public uint MatchId { get; set; }

		public RpsGame() { }

		public RpsGame(RpsChallenge challenge) : base(challenge.GuildChannelIds.Guild, challenge.GuildChannelIds.Channel, challenge.PlayerIds.Challenger, challenge.PlayerIds.Opponent, challenge.FirstToScore)
		{
			this.RoundCount = 1;
			this.Scores = new ChallengerOpponentPair<uint>(0, 0);
			this.Selections = new ChallengerOpponentPair<RpsSelection>(RpsSelection.None, RpsSelection.None);

			this.MatchId = GenerateId();
		}

		public RpsGame(ulong guildId, ulong channelId, ulong challengerId, ulong opponentId, uint firstToScore, RpsSelection botSelection)
					 : base(guildId, channelId, challengerId, opponentId, firstToScore)
		{
			this.RoundCount = 1;
			this.Scores = new ChallengerOpponentPair<uint>(0, 0);
			this.Selections = new ChallengerOpponentPair<RpsSelection>(RpsSelection.None, botSelection);

			this.MatchId = GenerateId();
		}


		public void IncrementRound()
		{
			this.RoundCount++;
		}
		public void ClearSelections(bool isBotMatch = false)
		{
			this.Selections = new ChallengerOpponentPair<RpsSelection>(RpsSelection.None, isBotMatch ? (RpsSelection)(1 << Util.Rand.Next(3)) : RpsSelection.None);
		}

		public static uint GenerateId()
		{
			uint id;
			do
			{
				id = (uint)Util.Rand.Next(0xffffff);
			} while (Util.Rps.GameExists(id));

			return id;
		}
	}

	public struct GuildChannel
	{
		public ulong Guild { get; set; }
		public ulong Channel { get; set; }

		public GuildChannel(ulong guildId, ulong channelId)
		{
			this.Guild = guildId;
			this.Channel = channelId;
		}
	}
	public class ChallengerOpponentPair<T>
	{
		// i tried to make this a struct. i really did. but im too small brain to make it work. this is the best i can give ya with my drained brainpower got. sorry.
		// idk maybe we can fix it later? "nothing more permanent etc. etc." but i mean. its fine. this is fine.
		// dont code on a broken brain kids -jolk 2022-01-09
		public T Opponent { get; set; }
		public T Challenger { get; set; }

		public ChallengerOpponentPair(T challenger, T opponent)
		{
			this.Challenger = challenger;
			this.Opponent = opponent;
		}

		public bool Contains(T value)
		{
			return Challenger.Equals(value) || Opponent.Equals(value);
		}
	}

	public enum DataKey
	{
		Challenges,
		ActiveGames,
		GameKeys
	}
}
