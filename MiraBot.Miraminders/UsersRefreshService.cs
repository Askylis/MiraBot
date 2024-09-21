using Microsoft.Extensions.Hosting;

namespace MiraBot.Miraminders
{
    public class UsersRefreshService : BackgroundService
    {
        private readonly UsersCache _cache;

        public UsersRefreshService(UsersCache cache)
        {
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _cache.RefreshCacheAsync();

                await Task.Delay(TimeSpan.FromHours(1), CancellationToken.None);
            }
        }
    }
}
