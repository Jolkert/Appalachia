using Appalachia.Utility;
using Appalachia.Utility.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Appalachia.Modules
{
	[Group, Name(Source)]
	public class RngModule : ModuleBase<SocketCommandContext>, IModuleBase
	{
		private const string Source = "Rng";
		private static readonly Random Rand = new Random();

		[Command("coinflip"), Alias("coin", "flip"), Name(Source + "/Flip")]
		public async Task CoinFlip([Remainder] string _ = "")
		{

			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("RNGesus says:")
				.WithDescription($"{Context.User.Mention}\'s coin landed on **{(Rand.Next(2) == 0 ? "heads" : "tails")}**")
				.WithColor(Context.Guild.GetColor());

			await Context.Channel.SendMessageAsync("", false, embed.Build());
		}

		[Command("user"), Alias("somone", "person"), RequireContext(ContextType.Guild), Name(Source + "/User")]
		public async Task SelectRandomUser([Remainder] string _ = "")
		{
			/*SocketVoiceChannel voiceChannel = (Context.User as SocketGuildUser).VoiceChannel;
			if (voiceChannel == null)
			{
				await Context.Channel.SendMessageAsync("", false, Util.GenerateErrorEmbed("You must be in a voice channel\nto use this command!").Build());
				return;
			}

			SocketGuildUser selectedUser = voiceChannel.Users.ToArray()[Rand.Next(voiceChannel.Users.Count)];*/

			IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> asyncUsers = Context.Guild.GetUsersAsync();
			List<IGuildUser> users = new List<IGuildUser>();
			await foreach (IReadOnlyCollection<IGuildUser> subcontainer in asyncUsers)
				foreach (IGuildUser user in subcontainer.Where(usr => !usr.IsBot))
					users.Add(user);

			IGuildUser selectedUser = users[Rand.Next(users.Count)];

			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("RNGesus says:")
				.WithDescription($"I have chosen\n{selectedUser.Mention}")
				.WithColor(Context.Guild.GetColor())
				.WithThumbnailUrl(selectedUser.GetGuildOrDefaultAvatarUrl());

			await Context.Channel.SendMessageAsync("", false, embed.Build());
		}

		

		[Group, Name(RngModule.Source + "/" + Source)]
		public class RollModule : ModuleBase<SocketCommandContext>, IModuleBase
		{
			private const string Source = "Roll";

			[Command("roll"), Alias("rng", "random", "rand", "randnum"), Name(RngModule.Source + "/" + Source)]
			public async Task BasicRng(int min, int max, int calls = 1)
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
					embed.WithDescription($"{Context.User.Mention} rolled\n{GenerateRollString(rolls, total, min, max)}");
					embed.WithColor(GetRollColor(total, min, max, calls));
				}

				await Context.Channel.SendMessageAsync("", false, embed.Build());
			}
			[Command("roll"), Alias("rng", "random", "rand", "randnum"), Name(Source)]
			public async Task BasicRng(int max)
			{
				await BasicRng(1, max);
			}
			[Command("roll"), Alias("rng", "random", "rand", "randnum"), Name(Source)]
			public async Task BasicRng()
			{
				await BasicRng(1, 100);
			}

			[Command("roll"), Alias("dice", "die"), Priority(int.MinValue), Name(RngModule.Source + "/" + Source + "/Dice")]
			public async Task DiceRoll([Remainder] string diceArgument)
			{
				await Program.LogAsync($"Attempting to parse dice from: \"{diceArgument}\"", Source);

				// This regex *should* be easy to parse, but I know I'm, stupid sometimes. Just remeber that '\d' is digit, '*' is 0-inf, and '+' is 1-inf. -jolk 2022-01-03
				// Part of me wonders if it should even be a regex, but like. 100% the easiest way to do it lol -jolk 2022-01-03
				Match match = Regex.Match(diceArgument, @"(?<count>\d*)d(?<max>\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
				if (!match.Success)
				{
					await Context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed("Could not parse dice format!").Build());
					return;
				}

				int max = int.Parse(match.Groups.GetValueOrDefault("max").Value);
				if (int.TryParse(match.Groups.GetValueOrDefault("count").Value, out int count))
					await BasicRng(1, max, count);
				else
					await BasicRng(max);
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
		}
		
		private static (int[] rolls, int total) Rng(int min, int max, int calls = 1)
		{
			int[] rolls = new int[calls];
			for (int i = 0; i < calls; i++)
				rolls[i] = Rand.Next(min, max + 1);
			int total = rolls.Sum();

			Program.LogAsync($"Roll: {calls}x[{min}, {max}] Result: {{{string.Join(", ", rolls)}}}={total}", Source);
			return (rolls, total);
		}
	}


}
