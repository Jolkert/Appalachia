using Appalachia.Data;
using Appalachia.Extensions;
using Appalachia.Services;
using Appalachia.Utility;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Appalachia
{
	class Program
	{
		public static DiscordSocketClient Client;
		public static readonly string Version = Assembly.GetAssembly(typeof(Program)).GetName().Version.ToString(3); // DONT FORGET TO CHANGE THIS WHEN YOU DO UPDATES. I KNOW YOU WILL. DONT FORGET -jolk 2022-01-09

		public static Logger Logger { get; private set; }
		private static readonly DailyTrigger MidnightTrigger = new DailyTrigger();
		public static readonly BotConfig Config = new BotConfig();

		static void Main()
		{
			try
			{
				new Program().StartAsync().GetAwaiter().GetResult();
			}
			catch (Exception exception)
			{// if theres an exception we want to log it to the file and just rethrow it -jolk 2022-05-01
				Logger.Critical("Something real bad just happened!", exception);
				throw;
			}
			finally
			{// this way if theres an error thrown, we still close the logger to make sure everything is good here -jolk 2022-05-01
				if (Config?.Settings.OutputLogsToFile ?? true)
					Logger.Close();
			}
		}

		public async Task StartAsync()
		{
			Logger = new Logger(Config.Settings.OutputLogsToFile);

			if (Config.Settings.Token == null || Config.Settings.Token == "BOT_TOKEN_GOES_HERE")
			{
				Logger.Error("Bot token not found. Make sure you have your bot token set in Resources/config.json or enter token now");
				Console.Write("Enter bot token: ");
				Config.SetToken(Console.ReadLine());
			}
			if (Config.Settings.CommandPrefix == null || Config.Settings.CommandPrefix == string.Empty)
			{
				Logger.Error("Command prefix not found. Make sure you have your command prefix set in Resources/config.json or enter prefix now");
				Console.Write("Enter bot prefix: ");
				Config.SetPrefix(Console.ReadLine());
			}
			if (!Config.Settings.OutputLogsToFile)
				Logger.Info("OutputLogsToFile false in config. Logs of bot activity will not be saved!");

			Logger.Info($"Starting Appalachia v{Version}");

			using ServiceProvider services = ConfigureServices();
			Client = services.GetRequiredService<DiscordSocketClient>();
			Client.Log += LogAsync;
			Client.Ready += OnReadyAsync;
			Client.JoinedGuild += OnGuildJoinAsync;
			Client.LeftGuild += OnGuildLeaveAsync;
			Client.ReactionAdded += OnReactAsync;
			Client.MessageReceived += FilterWordsAsync;
			Client.UserJoined += AddDefaultRole;

			services.GetRequiredService<CommandService>().Log += LogAsync;
			await Client.LoginAsync(TokenType.Bot, Config.Settings.Token);
			await Client.StartAsync();
			await services.GetRequiredService<CommandHandler>().InitializeAsync();

			MidnightTrigger.Trigger += Logger.Restart;
			await Task.Delay(-1);
		}

		private async Task OnReadyAsync()
		{
			await Client.SetGameAsync($"{Config.Settings.CommandPrefix}help", null, ActivityType.Listening);

			List<ulong> activeGuilds = new List<ulong>();
			foreach (SocketGuild guild in Client.Guilds)
			{
				activeGuilds.Add(guild.Id);
				Logger.Info($"Connected to {guild.Name} ({guild.Id})");
				Task _ = guild.DownloadUsersAsync();
				if (!Util.Guilds.Exists(guild.Id))
				{
					(ulong announcementChannelId, ulong quoteChannelId) = guild.CheckForImportantChannels();
					Util.Guilds.AddGuild(guild.Id, announcementChannelId, quoteChannelId);
				}
			}

			int guildsRemoved = Util.Guilds.RemoveMissingIds(activeGuilds.ToArray());
			if (guildsRemoved > 0)
				Logger.Info($"Removed {guildsRemoved} extraneous guild{(guildsRemoved != 1 ? "s" : "")} from database");

			Logger.Info($"Bot is active in {Client.Guilds.Count} guild{(Client.Guilds.Count != 1 ? "s" : "")}!");
		}

		private async Task OnReactAsync(Cacheable<IUserMessage, ulong> messageArg, Cacheable<IMessageChannel, ulong> channelArg, SocketReaction reaction)
		{
			if (reaction.User.Value.IsBot)
				return;

			Logger.Verbose($"{reaction.User.Value?.GetFullUsername()} reacted {reaction.Emote.ToDiscordName()} in {reaction.Channel.GetGuildChannelName()}/{reaction.MessageId}");

			// i mean technically i shouldnt be using the discard i think? but w/e
			ReactionStatus status = reaction.GetStatus();
			if ((status & ReactionStatus.RpsConfirmations) != 0)
			{
				IMessageChannel channel = await channelArg.DownloadAsync();

				RpsChallenge challenge = Util.Rps.GetChallenge(reaction.MessageId);
				// so like. doing this async kinda broke stuff before. but i think that was just because i was storing the challenge *after* reacting? we should be fine now?
				// like you'd have to be the faster mfer ever to hit a react before the challenge gets stored. so i think its probably fine -jolk 2022-01-08
				if (reaction.UserId == challenge?.PlayerIds.Opponent)
					_ = HandleRpsConfirmationAsync(challenge, status, channel, reaction);
			}
			else if ((status & ReactionStatus.RpsSelections) != 0)
			{// im learning how to actually write ansync code in like the very middle of Appalachia 2.0 oh god -jolk 2022-01-11
				Task<IMessageChannel> channelTask = channelArg.DownloadAsync();
				IUserMessage message = await messageArg.DownloadAsync();

				RpsGame game = Util.Rps.GetActiveGame(message.Embeds.FirstOrDefault().ParseGameId());
				// more async babyyy -jolk 2022-01-09
				// im actuallly worried about this one. theres a real chance that something goes terribly wrong trying to run this asynchronously. im sure its fine -jolk 2022-01-09
				if (game != null && game.PlayerIds.Contains(reaction.UserId))
					_ = HandleRpsSelectionAsync(game, status, await channelTask, reaction);
			}
		}

		private Task OnGuildJoinAsync(SocketGuild guild)
		{
			Logger.Info($"Joined {guild.Name} ({guild.Id})");
			Task _ = guild.DownloadUsersAsync();
			(ulong announcementChannelId, ulong quoteChannelId) = guild.CheckForImportantChannels();
			Util.Guilds.AddGuild(guild.Id, announcementChannelId, quoteChannelId);
			return Task.CompletedTask;
		}
		private Task OnGuildLeaveAsync(SocketGuild guild)
		{
			Util.Guilds.RemoveGuild(guild.Id);
			Logger.Info($"Removed data for {guild.GetNameWithId()}");
			return Task.CompletedTask;
		}

		private async Task FilterWordsAsync(SocketMessage message)
		{
			if (message.Source == MessageSource.User && message.Channel is not SocketDMChannel && message.HasFilteredWord())
			{
				try
				{
					await message.DeleteAsync();
					Logger.Info($"Removed message \"{message.Content}\" from {message.Author.GetFullUsername()} in {message.Channel.GetGuildChannelName()}");
				}
				catch (Discord.Net.HttpException)
				{
					Logger.Error($"Attempted but unable to remove message \"{message.Content}\" from {message.Author.GetFullUsername()} in {message.Channel.GetGuildChannelName()}");
				}

			}
		}

		private async Task AddDefaultRole(SocketGuildUser user)
		{
			SocketRole role = user.Guild.GetDefaultRole();
			if (role == null)
				return;

			await user.AddRoleAsync(role);
			Logger.Info($"Added default role {role.GetNameWithId()} to user new user {user.GetFullUsername()} in {user.Guild.GetNameWithId()}");
		}


		private static async Task HandleRpsConfirmationAsync(RpsChallenge challenge, ReactionStatus status, IMessageChannel rawChannel, SocketReaction reaction)
		{
			if (rawChannel is not SocketTextChannel channel) // in practice this should never happen? but like. just in case -jolk 2022-01-10
				return;

			Logger.Verbose($"[{reaction.User.Value.GetFullUsername()}] reacted [{status}] in [{channel.GetGuildChannelName()}/{reaction.MessageId}]");

			SocketGuildUser challenger = channel.Guild.GetUser(challenge.PlayerIds.Challenger);
			SocketGuildUser opponent = channel.Guild.GetUser(challenge.PlayerIds.Opponent);

			EmbedBuilder embed = new EmbedBuilder().WithColor(channel.Guild.GetColor());
			bool accepted = false;
			switch (status)
			{
				case ReactionStatus.RpsAccept:
					embed.WithTitle("Challenge accepeted!")
						.WithDescription($"{opponent.Mention} has accepted {challenger.Mention}\'s challenge!");
					accepted = true;
					break;

				case ReactionStatus.RpsDeny:
					embed.WithTitle("Challenge declined!")
						.WithDescription($"{opponent.Mention} has declined {challenger.Mention}\'s challenge!");
					break;

				default:
					await rawChannel.SendErrorMessageAsync("This isn\'t supposed to happen.\nIf you see this something has gone terribly wrong.");
					Logger.Error("If you\'re seeing this, something is terribly wrong with HandleRpsConfirmation");
					return;
			}

			await rawChannel.SendEmbedAsync(embed);
			Task _;
			if (accepted)
				_ = Task.Run(() => SendPvpSelectionMessages(challenger, opponent, challenge));

			Util.Rps.RemoveChallenge(reaction.MessageId);
		}
		private static async Task HandleRpsSelectionAsync(RpsGame gameData, ReactionStatus status, IMessageChannel downloadedChannel, SocketReaction reaction)
		{
			// im wondering if i should like. take a break to clear my brain. ive been staring at vs for too long. im starting to feel the brainpower leaving my soul
			// and like. i really *shouldnt* write this method in particular in this state. yea im gonna take a break -jolk 2022-01-09 (01:49)

			Logger.Verbose($"[{reaction.User.Value.GetFullUsername()}] reacted [{status}] in [{downloadedChannel.GetGuildChannelName()}/{reaction.MessageId}] [#{gameData.MatchId:x6}]");

			bool isBotMatch = gameData.PlayerIds.Contains(Client.CurrentUser.Id);
			bool userIsChallenger = reaction.UserId == gameData.PlayerIds.Challenger;
			if ((userIsChallenger ? gameData.Selections.Challenger : gameData.Selections.Opponent) != RpsSelection.None)
				// sorry future me. i know parsing this will be a bit annoying. it's the "player hasnt already reacted" check. trust -jolk 2022-01-09
				return;

			if (userIsChallenger)
			{
				RpsSelection userSelection = status.GetUserSelection();

				gameData.SetChallengerSelection(userSelection);
				Logger.Verbose($"Challenger selection: [{userSelection}] (#{gameData.MatchId:x6})");
			}
			else
			{
				RpsSelection userSelection = status.GetUserSelection();

				gameData.SetOpponentSelection(userSelection);
				Logger.Verbose($"Opponent selection: [{userSelection}] (#{gameData.MatchId:x6})");
			}

			// i cant. see above comment. i need to rest my brain. ill do the rest later -jolk 2022-01-09
			// time to game -jolk 2022-01-09 (15:30)

			if (!gameData.Selections.Contains(RpsSelection.None))
			{
				SocketGuild guild = Client.GetGuild(gameData.GuildChannelIds.Guild);
				SocketTextChannel channel = guild.GetTextChannel(gameData.GuildChannelIds.Channel);
				SocketGuildUser challenger = guild.GetUser(gameData.PlayerIds.Challenger), opponent = guild.GetUser(gameData.PlayerIds.Opponent);

				RpsWinner roundWinner = gameData.Selections.RpsLogic();

				// determine winner of match
				RpsWinner matchWinner = RpsWinner.Undecided;
				switch (roundWinner)
				{
					case RpsWinner.Challenger:
						if (gameData.IncrementChallengerScore() >= gameData.FirstToScore)
							matchWinner = RpsWinner.Challenger;
						break;

					case RpsWinner.Opponent:
						if (gameData.IncrementOpponentScore() >= gameData.FirstToScore)
							matchWinner = RpsWinner.Opponent;
						break;

					default:
						break;
				}

				await channel.SendEmbedAsync(GenerateRoundResultEmbed(gameData, challenger, opponent, roundWinner));

				// send match winner if determined
				gameData.IncrementRound();
				(int winner, int loser) previousElos = (-1, -1);
				switch (matchWinner)
				{
					// there has to be a more sensible way to do this than this. it looks stupid but i dont feel like trying to make it better so this is what we got yall -jolk 2022-02-14	
					case RpsWinner.Challenger:

						gameData.RemoveFromDatabase();
						if (!isBotMatch)
						{
							challenger.IncrementRpsWins();
							opponent.IncrementRpsLosses();

							previousElos = Util.Guilds.UpdateElo(challenger, opponent);
						}

						await channel.SendEmbedAsync(GenerateMatchResultEmbed(gameData, challenger, opponent, previousElos));
						break;

					case RpsWinner.Opponent:
						gameData.RemoveFromDatabase();
						if (!isBotMatch)
						{
							opponent.IncrementRpsWins();
							challenger.IncrementRpsLosses();

							previousElos = Util.Guilds.UpdateElo(opponent, challenger);
						}

						await channel.SendEmbedAsync(GenerateMatchResultEmbed(gameData, opponent, challenger, previousElos));
						break;

					default:
						gameData.ResetSelections(isBotMatch);
						if (!isBotMatch)
							SendPvpSelectionMessages(challenger, opponent, gameData);
						else
						{
							Logger.Verbose($"Appalachia selects [{gameData.Selections.Opponent}] in match [#{gameData.MatchId:x6}] against [{challenger.GetFullUsername()}]");
							Task _ = (await SendBotSelectionMessage(channel, challenger, gameData)).AddRpsReactionsAsync();
						}
						break;
				}
			}
		}

		// this is a complete mess but it works so its okay -jolk 2022-07-07
		// TODO: fix this lmao
		private static void SendPvpSelectionMessages(SocketGuildUser challenger, SocketGuildUser opponent, RpsChallenge challenge)
		{// ive just made this method that supposed to be async just kinda like. not lol -jolk 2022-01-13
			if (challenge is not RpsGame gameData)
			{
				gameData = new RpsGame(challenge);
				gameData.AddToDatabase();
			}

			IUserMessage challengerMessage = null;
			IUserMessage opponentMessage = null;


			Task[] tasks = new Task[]
			{
					Task.Run(async () =>
					{
						challengerMessage = await challenger.SendMessageAsync("[Please wait...]");
						Task _ =  challengerMessage.AddRpsReactionsAsync();
					}),
					Task.Run(async () =>
					{
						opponentMessage = await opponent.SendMessageAsync("[Please wait...]");
						Task _ = opponentMessage.AddRpsReactionsAsync();
					})
			};
			Task.WaitAll(tasks); // after a bit of testing, it seems like doing this actually asynchronously is like. a decent amount faster. go figure -jolk 2022-01-08

			// should i do this in parallel? yea fuck it why not? -jolk 2022-01-08
			Parallel.Invoke(
				async () => await challengerMessage.ModifyAsync(msg =>
				{
					msg.Content = "";
					msg.Embed = GenerateSelectionMessageEmbed(gameData, opponent).Build();
				}),
				async () => await opponentMessage.ModifyAsync(msg =>
				{
					msg.Content = "";
					msg.Embed = GenerateSelectionMessageEmbed(gameData, challenger).Build();
				}));
		}
		public static async Task<IMessage> SendBotSelectionMessage(ISocketMessageChannel channel, IUser challenger, RpsGame gameData)
		{// hey if something goes fucky check here first lol -jolk 2022-03-28
			return await channel.SendEmbedAsync(new EmbedBuilder().WithTitle($"Round {gameData.RoundCount} vs {challenger.Username}")
																	  .WithDescription($"{challenger.Mention}, select rock, paper, or scissors")
																	  .WithColor(gameData.MatchId)
																	  .WithThumbnailUrl(challenger.GetGuildOrDefaultAvatarUrl())
																	  .WithFooter($"Match ID: #{gameData.MatchId:x6}"));
		}

		private static EmbedBuilder GenerateMatchResultEmbed(RpsGame gameData, SocketGuildUser winner, SocketGuildUser loser, (int winner, int loser) previousElos)
		{
			(int winner, int loser) newElos = (winner.GetGuildRpsScore().Elo, loser.GetGuildRpsScore().Elo);

			string description = $"{winner.Mention} wins the set!";
			if (previousElos.winner != -1 && previousElos.loser != -1)
				description += $"\n\n{winner.Mention}: {previousElos.winner} → {newElos.winner} (+{newElos.winner - previousElos.winner})\n" +
										$"{loser.Mention}: {previousElos.loser} → {newElos.loser} (-{previousElos.loser - newElos.loser})";

			return new EmbedBuilder().WithTitle("Game, Set, and Match!")
					  .WithDescription(description)
					  .WithColor(gameData.MatchId)
					  .WithThumbnailUrl(winner.GetGuildOrDefaultAvatarUrl())
					  .WithFooter($"Match ID: #{gameData.MatchId:x6}");
		}
		private static EmbedBuilder GenerateRoundResultEmbed(RpsGame gameData, SocketGuildUser challenger, SocketGuildUser opponent, RpsWinner winner)
		{
			SocketGuildUser winningUser = winner switch
			{
				RpsWinner.Challenger => challenger,
				RpsWinner.Opponent => opponent,
				_ => null
			};

			return new EmbedBuilder()
						.WithTitle($"Round {gameData.RoundCount}!")
						.WithDescription($"{GenerateUserSelectionString(challenger, gameData.Selections.Challenger)}\n" +
										 $"{GenerateUserSelectionString(opponent, gameData.Selections.Opponent)}\n\n" +
										 $"Round {gameData.RoundCount} winner: {winningUser?.Mention ?? "Draw"}\n" +
										 $"Score: `{gameData.Scores.Challenger} - {gameData.Scores.Opponent}`")
						.WithColor(gameData.MatchId)
						.WithThumbnailUrl(winningUser.GetGuildOrDefaultAvatarUrl())
						.WithFooter($"Match ID: #{gameData.MatchId:x6}");
		}
		private static EmbedBuilder GenerateSelectionMessageEmbed(RpsGame gameData, IUser opponent)
		{
			return new EmbedBuilder().WithTitle($"Round {gameData.RoundCount} vs {opponent.Username}")
				.WithDescription("Select rock, paper, or scissors")
				.WithColor(gameData.MatchId)
				.WithThumbnailUrl(opponent.GetAvatarUrl())
				.WithFooter($"Match ID: #{gameData.MatchId:x6}");
		}

		private static string GenerateUserSelectionString(SocketGuildUser user, RpsSelection selection)
		{
			return $"{user.Mention} chose {selection} ({selection.ToEmote()})";
		}



		private Task LogAsync(LogMessage message)
		{
			Logger.Log(message);
			return Task.CompletedTask;
		}

		public static void Stop()
		{
			Logger.Close();
			Environment.Exit(Environment.ExitCode);
		}



		private static ServiceProvider ConfigureServices()
		{
			return new ServiceCollection()
				.AddSingleton<DiscordSocketClient>(new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All }))
				.AddSingleton<CommandService>()
				.AddSingleton<CommandHandler>()
				.BuildServiceProvider();
		}
	}
}
