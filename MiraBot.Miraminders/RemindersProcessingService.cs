using Microsoft.Extensions.Hosting;
using MiraBot.DataAccess;
using System.Diagnostics;

namespace MiraBot.Miraminders
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
                var reminders = await _cache.GetActiveReminder();
                if (reminders is not null && reminders.Count > 0)
                {
                    List<Reminder> completedReminders = new();
                    foreach (var reminder in reminders)
                    {
                        Console.WriteLine($"Reminder for {reminder.RecipientName}! Reminder message: {reminder.Message}.");
                        //SendReminderAsync
                        completedReminders.Add(reminder);
                    }
                    reminders.RemoveAll(completedReminders.Contains);
                    await _cache.RefreshCache();
                }
                refreshCounter++;
                if (refreshCounter >= 600)
                {
                    await _cache.RefreshCache();
                    Console.WriteLine("Refreshed cache!");
                    refreshCounter = 0;
                }
                await Task.Delay(1000);
            }
        }
    }
}
