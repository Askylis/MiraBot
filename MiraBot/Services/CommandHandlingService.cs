using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace MiraBot.Services
{
    public class CommandHandlingService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public CommandHandlingService(DiscordSocketClient client, CommandService commands,
            IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived -= HandleCommandAsync;
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (messageParam is not SocketUserMessage message) return;
            if (message.Author.IsBot) return;

            int argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"Command execution failed: {result.ErrorReason}");
            }
        }
    }
}
