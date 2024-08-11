using Microsoft.Extensions.Hosting;
using MiraBot.Miraminders;
using System.Diagnostics;

namespace MiraBot.Services
{
    public class RemindersProcessingService : BackgroundService
    {
        private readonly RemindersCache _cache;
        public RemindersProcessingService(RemindersCache cache) 
        { 
            _cache = cache;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Debug.WriteLine("Processing service executed.");
            await _cache.RefreshCache();
            int refreshCounter = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
                refreshCounter++;
                if (refreshCounter >= 600)
                {
                    await _cache.RefreshCache();
                    refreshCounter = 0;
                }
            }
        }
    }
}
