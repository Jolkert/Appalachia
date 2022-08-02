﻿using Appalachia.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Appalachia.Services
{
	class CommandHandler
	{
		public static CommandService Commands { get; private set; }
		public static Dictionary<ulong, Task> RunningCommands { get; } = new Dictionary<ulong, Task>(20);

		private readonly CommandService _commands;
		private readonly DiscordSocketClient _client;
		private readonly IServiceProvider _services;

		public CommandHandler(IServiceProvider services)
		{
			_commands = services.GetRequiredService<CommandService>();
			_client = services.GetRequiredService<DiscordSocketClient>();
			_services = services;

			_commands.CommandExecuted += CommandExecutedAsync;
			_client.MessageReceived += MessageReceivedAsync;

			Commands = _commands;
		}

		public async Task InitializeAsync()
		{
			await _commands.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _services);
		}

		private Task MessageReceivedAsync(SocketMessage rawMessage)
		{
			if (Program.IsStopping)
				return Task.CompletedTask;

			if (rawMessage is not SocketUserMessage message)
				return Task.CompletedTask;

			if (message.Source != MessageSource.User)
				return Task.CompletedTask;

			int argPos = 0;
			if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasStringPrefix(Program.Config.Settings.CommandPrefix, ref argPos)))
				return Task.CompletedTask;

			RunningCommands.Add(message.Id, Task.Run(async () =>
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				SocketCommandContext context = new SocketCommandContext(_client, message);
				IResult result = await _commands.ExecuteAsync(context, argPos, _services);
				stopwatch.Stop();

				if (result.IsSuccess)
					Program.Logger.Info($"Command took {stopwatch.ElapsedMilliseconds} ms");

				RunningCommands.Remove(context.Message.Id);
			}));
			

			return Task.CompletedTask;
		}

		private Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
		{
			if (!command.IsSpecified)
				Program.Logger.Info($"Unknown Command! [{context.User.GetFullUsername()}] in [{context.GetGuildChannelName()}] / [{context.Message}]");
			else if (result.IsSuccess)
				Program.Logger.Info($"[{context.User.GetFullUsername()}] ran [{command.Value.Name}] in [{context.GetGuildChannelName()}]");
			else
				Program.Logger.Error($"An error occured while attempting to run a command! [{context.User.GetFullUsername()}] in [{context.GetGuildChannelName()}] / [{result}]");

			return Task.CompletedTask;
		}
	}
}