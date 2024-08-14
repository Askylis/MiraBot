using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using MiraBot.DataAccess;

namespace MiraBot.Miraminders
{
    public class RemindersProcessingService : BackgroundService
    {
        private readonly RemindersCache _cache;
        private readonly DiscordSocketClient _client;
        private readonly Miraminder _reminder;
        public RemindersProcessingService(RemindersCache cache, DiscordSocketClient client, Miraminder reminder) 
        { 
            _cache = cache;
            _client = client;
            _reminder = reminder;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
            var user = await _reminder.GetUserAsync(reminder.RecipientId);
            var recipient = await _client.Rest.GetUserAsync(user.DiscordId);
            var dm = await recipient.CreateDMChannelAsync();
            if (reminder.OwnerId == reminder.RecipientId)
            {
                await dm.SendMessageAsync($"Here's your reminder! The message attached to it is this: \"{reminder.Message}\"");
            }
            else
            {
                var owner = await _reminder.GetUserAsync(reminder.OwnerId);
                var ownerDiscord = await _client.Rest.GetUserAsync(owner.DiscordId);
                var ownerDm = await ownerDiscord.CreateDMChannelAsync();
                try
                {
                    await dm.SendMessageAsync($"You have a reminder from {owner.UserName}! The message attached to this reminder is: \"{reminder.Message}\"");
                    await ownerDm.SendMessageAsync($"I just sent your reminder to {recipient.Username}! Your reminder contained the following message: \"{reminder.Message}\"");
                }
                catch
                {
                    await ownerDm.SendMessageAsync($"Your reminder to {recipient.Username} failed to send. This is likely due to their privacy settings.");
                }
            }
        }
    }
}
