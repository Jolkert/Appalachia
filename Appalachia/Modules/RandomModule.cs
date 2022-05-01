using Appalachia.Utility;
using Appalachia.Utility.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	[Group, Name(Source)]
	public class RandomModule : ModuleBase<SocketCommandContext>, IModuleBase
	{
		private const string Source = "RandomModule";

		[Group("flip"), Alias("coin", "coinflip"), Name(Source)]
		public class FlipModule : ModuleWithHelp
		{
			private const string Source = "Flip";

			public override string ModuleName => Source;
			public override string Description => "Flips a coin";
			public override string Usage => "";

			[Command]
			public async Task CoinFlip()
			{
				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle("RNGesus says:")
					.WithDescription($"{Context.User.Mention}\'s coin landed on **{(Util.Rand.Next(2) == 0 ? "heads" : "tails")}**")
					.WithColor(Context.Guild.GetColor());

				await Context.Message.ReplyEmbedAsync(embed);
			}
		}

		[Group("randomuser"), Alias("randuser", "user", "someone", "person"), RequireContext(ContextType.Guild), Name(Source)]
		public class RandomUserModule : ModuleWithHelp
		{
			private const string Source = "Random User";

			public override string ModuleName => Source;
			public override string Description => "Selects a random user. If you are not in a voice channel, or use the `--server` flag while in a voice channel, " +
												  "any non-bot user in the server may be selected. If you are in a voice channel and do not use the `--server` flag, any non-bot user " +
												  "in the voice channel may be selected";
			public override string Usage => "[-s|--server]";

			[Command]
			public async Task SelectRandomUser(string flag = "") // i really dont want to make a subcommand. im just gonna use this lmao -jolk 2022-04-19
			{
				if (Context.User is not SocketGuildUser contextUser) // i dont think you ever really need this but sure -jolk 2022-04-21
					return;

				IGuildUser selectedUser;
				if (contextUser.VoiceChannel == null || Regex.IsMatch(flag, @"(\b|^)(-s|--server)(\b|$)", RegexOptions.IgnoreCase))
				{
					IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> asyncUsers = Context.Guild.GetUsersAsync();
					List<IGuildUser> users = new List<IGuildUser>();
					await foreach (IReadOnlyCollection<IGuildUser> subcontainer in asyncUsers)
						foreach (IGuildUser user in subcontainer.Where(usr => !usr.IsBot))
							users.Add(user);

					selectedUser = users[Util.Rand.Next(users.Count)];
				}
				else
				{
					SocketGuildUser[] users = contextUser.VoiceChannel.Users.Where(usr => !usr.IsBot).ToArray();
					selectedUser = users[Util.Rand.Next(users.Length)];
				}

				EmbedBuilder embed = new EmbedBuilder()
						.WithTitle("RNGesus says:")
						.WithDescription($"I have chosen\n{selectedUser.Mention}")
						.WithColor(Context.Guild.GetColor())
						.WithThumbnailUrl(selectedUser.GetGuildOrDefaultAvatarUrl());

				await Context.Message.ReplyEmbedAsync(embed);
			}
		}

		[Group("rng"), Alias("random", "rand", "randnum"), Name(Source)]
		public class RngModule : ModuleWithHelp
		{
			private const string Source = "Rng";

			public override string ModuleName => Source;
			public override string Description => "Generate a random number (default range: [1, 100])";
			public override string Usage => "[min] [max] [count]";

			[Command]
			public async Task BasicRng(int min, int max, int calls = 1)
			{
				await Context.Message.ReplyEmbedAsync(GenerateRolls(Context, min, max, calls));
			}
			[Command]
			public async Task BasicRng(int max)
			{
				await BasicRng(1, max);
			}
			[Command]
			public async Task BasicRng()
			{
				await BasicRng(1, 100);
			}
		}

		[Group("roll"), Alias("dice", "die"), Name(Source)]
		public class RollModule : ModuleWithHelp
		{
			private const string Source = "Roll";

			public override string ModuleName => Source;
			public override string Description => "Rolls a die or dice using [standard dice notation](https://en.wikipedia.org/wiki/Dice_notation#Standard_notation) (defaults to 1d100)";
			public override string Usage => "[dice_notation]";

			[Command]
			public async Task DiceRoll([Remainder] string diceArgument)
			{
				await Program.LogAsync($"Attempting to parse dice from: \"{diceArgument}\"", Source, LogSeverity.Debug);

				// This regex *should* be easy to parse, but I know I'm, stupid sometimes. Just remeber that '\d' is digit, '*' is 0-inf, and '+' is 1-inf. -jolk 2022-01-03
				// Part of me wonders if it should even be a regex, but like. 100% the easiest way to do it lol -jolk 2022-01-03
				Match match = Regex.Match(diceArgument, @"(?<count>\d*)d(?<max>\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
				if (!match.Success)
				{
					await Context.Channel.SendEmbedAsync(EmbedHelper.GenerateErrorEmbed("Could not parse dice format!"));
					return;
				}

				int max = int.Parse(match.Groups.GetValueOrDefault("max").Value);
				if (int.TryParse(match.Groups.GetValueOrDefault("count").Value, out int count))
					await Context.Message.ReplyEmbedAsync(GenerateRolls(Context, 1, max, count));
				else
					await Context.Message.ReplyEmbedAsync(GenerateRolls(Context, 1, max));
			}
		}


		private static EmbedBuilder GenerateRolls(SocketCommandContext context, int min, int max, int calls = 1)
		{
			EmbedBuilder embed = new EmbedBuilder();

			if (max <= min)
				embed = EmbedHelper.GenerateErrorEmbed("Max value must be greater than min value!");
			else if (calls < 1)
				embed = EmbedHelper.GenerateErrorEmbed("Why would you even try that?");
			else
			{
				(int[] rolls, int total) = Rng(min, max, calls);

				embed.WithTitle("RNGesus says:");
				embed.WithDescription($"{context.User.Mention} rolled\n{GenerateRollString(rolls, total, min, max)}");
				embed.WithColor(GetRollColor(total, min, max, calls));
			}

			return embed;
		}
		private static string GenerateRollString(int[] rolls, int total, int min, int max)
		{
			string rollsString = string.Join(" / ", rolls.Select(n =>
			{
				if (n == max)
					return $"**{n:#,###}**";
				else if (n == min)
					return $"*{n:#,###}*";
				else
					return n.ToString("#,###");
			}));

			if (rolls.Length > 1)
				rollsString += $"\n**Total: {total}**";

			return rollsString;
		}
		private static uint GetRollColor(int total, int min, int max, int calls)
		{
			double averageValue = calls * (min + max) / 2.0;
			(int lowerAvg, int higherAvg) = (int)averageValue == averageValue ? ((int)averageValue, (int)averageValue) : ((int)(averageValue - .5), (int)(averageValue + .5));


			// no switch sadge. smh my head -jolk 2022-01-12
			if (total == lowerAvg || total == higherAvg)
				return Colors.AvgRoll;
			else if (total > min * calls && total < lowerAvg)
				return Colors.BadRoll;
			else if (total < max * calls && total > higherAvg)
				return Colors.GoodRoll;
			else if (total == min * calls)
				return Colors.MinRoll;
			else if (total == max * calls)
				return Colors.MaxRoll;
			else
			{
				Program.LogAsync("You should not see this. If you see this, RngModule.RollModule.GetRollColor() is broken. Go fix that.", Source);
				return Colors.Default;
			}
		}
		private static (int[] rolls, int total) Rng(int min, int max, int calls = 1)
		{
			int[] rolls = new int[calls];
			for (int i = 0; i < calls; i++)
				rolls[i] = Util.Rand.Next(min, max + 1);
			int total = rolls.Sum();

			Program.LogAsync($"Roll: {calls}x[{min}, {max}] Result: {{{string.Join(", ", rolls)}}}={total}", Source);
			return (rolls, total);
		}
	}
}
