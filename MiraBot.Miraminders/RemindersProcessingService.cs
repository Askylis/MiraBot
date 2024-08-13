using Microsoft.Extensions.Hosting;
using MiraBot.DataAccess;
using System.Diagnostics;
using Discord;
using Discord.WebSocket;

namespace MiraBot.Miraminders
{
    public class RemindersProcessingService : BackgroundService
    {
        private readonly RemindersCache _cache;
        private readonly DiscordSocketClient _client;
        public RemindersProcessingService(RemindersCache cache, DiscordSocketClient client) 
        { 
            _cache = cache;
            _client = client;
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
                        await SendReminderAsync(reminder);
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

        private async Task SendReminderAsync(Reminder reminder)
        {
            var user = await _cache.GetUserAsync(reminder.RecipientId);
            var recipient = _client.GetUser(user.DiscordId);
            var dm = await recipient.CreateDMChannelAsync();
            if (reminder.OwnerId == reminder.RecipientId)
            {
                await dm.SendMessageAsync($"Here's your reminder! The message attached to it is this: \"{reminder.Message}\"");
            }
            else
            {
                await dm.SendMessageAsync($"You have a reminder from {reminder.OwnerId}! The message attached to this reminder is: \"{reminder.Message}\"");
            }
        }
    }
}
