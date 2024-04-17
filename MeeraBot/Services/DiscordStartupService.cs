using Discord;
using Discord.WebSocket;
using MeeraBot.Options;
using MeeraBot.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace MeeraBot.Services
{
    public class DiscordStartupService : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly DiscordOptions _config;
        private readonly ILogger<DiscordSocketClient> _logger;

        public DiscordStartupService(DiscordSocketClient discord, IOptions<DiscordOptions> config, ILogger<DiscordSocketClient> logger)
        {
            _discord = discord;
            _config = config.Value;
            _logger = logger;

            _discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _discord.LoginAsync(TokenType.Bot, _config.Token);
            await _discord.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discord.LogoutAsync();
            await _discord.StopAsync();
        }
    }
}
