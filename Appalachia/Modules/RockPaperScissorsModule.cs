using Appalachia.Data;
using Appalachia.Utility;
using Appalachia.Utility.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	[Group("rps"), RequireContext(ContextType.Guild), Name(Source)]
	public class RockPaperScissorsModule : ModuleBase<SocketCommandContext>, IModuleBase, IModuleWithHelp
	{
		public const string Source = "Rock Paper Scissors";
		public string ModuleName => Source;

		// TODO: theres a bunch of stuff going on here. a bunch of the message sending stuff should probably be refactored for cleanup. but like. not rn -jolk 2022-01-10
		[Command, Name(Source)]
		public async Task RockPaperScissorsCommand(SocketGuildUser opponent = null, uint firstToScore = 1)
		{
			// opponent ??= Context.Guild.GetUser(Program.Client.CurrentUser.Id);

			if (opponent == null)
			{
				await HelpCommand();
				return;
			}

			if (opponent.IsBot)
			{
				if (opponent.Id == Program.Client.CurrentUser.Id)
				{
					EmbedBuilder embed = new EmbedBuilder().WithTitle("Challenge accepted!")
											.WithDescription($"I accept {Context.User.Mention}\'s challenge!")
											.WithColor(Context.Guild.GetColor());

					await Context.Channel.SendMessageAsync("", false, embed.Build());

					RpsSelection botSelection = (RpsSelection)(1 << Util.Rand.Next(3));
					RpsGame gameData = new RpsGame(Context.Guild.Id, Context.Channel.Id, Context.User.Id, opponent.Id, firstToScore, botSelection);
					await Program.LogAsync($"Appalachia selects [{botSelection}] in match [#{gameData.MatchId:x6}] against [{Context.User.GetFullUsername()}]", Source);

					gameData.AddToDatabase();
					IMessage message = await Program.SendBotSelectionMessage(Context.Channel, Context.User, gameData);

					await message.AddRpsReactionsAsync();
				}
				else //TODO: get a better error message maybe? -jolk 2022-01-07
					await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed("I am the only bot smart enough to play this game.\nsorry").Build());
			}
			else if (Context.User.Id == opponent.Id)
				await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed("You cannot challenge yourself!").Build());
			else
			{
				EmbedBuilder embed = new EmbedBuilder().WithTitle($"{opponent.Nickname ?? opponent.Username}, do you accept the challenge?")
										.WithDescription($"{Context.User.Mention} challenges {opponent.Mention} to a Rock Paper Scissors match!{(firstToScore > 1 ? $"\n(First to {firstToScore} wins)" : "")}")
										.WithColor(Context.Guild.GetColor());


				IMessage message = await Context.Channel.SendMessageAsync("", false, embed.Build());
				new RpsChallenge(Context.Guild.Id, Context.Channel.Id, Context.User.Id, opponent.Id, firstToScore).AddToDatabase(message.Id);

				await message.AddReactionAsync(Reactions.RpsAccept);
				await message.AddReactionAsync(Reactions.RpsDeny);
			}
		}
		[Command("leaderboard"), Alias("lb", "scores"), Name(Source + "/Leaderboard")]
		public async Task RockPaperScissorsLeaderboard(SocketGuildUser userFilter = null)
		{
			if (userFilter == null)
			{
				await Context.Channel.SendMessageAsync("aint done yet.");			}
			else
			{
				Server.Score userScore = userFilter.GetGuildRpsScore();
				EmbedBuilder embed = new EmbedBuilder().WithTitle("Rock Paper Scissors Stats")
										   .WithDescription($"Stats for {userFilter.Mention}\n")
										   .WithFields(new EmbedFieldBuilder[]
										   {
											   new EmbedFieldBuilder().WithName("Rank in Server").WithValue(userFilter.GetGuildRpsRank().ToOrdinal()).WithIsInline(false),
											   new EmbedFieldBuilder().WithName("Wins").WithValue(userScore.Wins).WithIsInline(true),
											   new EmbedFieldBuilder().WithName("Losses").WithValue(userScore.Losses).WithIsInline(true),
											   new EmbedFieldBuilder().WithName("Win Rate").WithValue($"{userScore.WinRate * 100 : 0.0}%").WithIsInline(true)
										   })
										   .WithColor(Context.Guild.GetColor())
										   .WithThumbnailUrl(userFilter.GetGuildOrDefaultAvatarUrl());

				await Context.Channel.SendMessageAsync("", false, embed.Build());
			}
		}	

		[Command("help"), Alias("?"), Name(Source + "/Help")]
		public async Task HelpCommand()
		{
			string description = "Play rock paper scissors against another user in the server, or this bot.";
			string usage = "<@user> [first_to_score]";

			await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateHelpEmbed(description, usage, this).Build());
		}
	}
}
