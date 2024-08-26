using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using MiraBot.DataAccess;

namespace MiraBot.Miraminders
{
    public class RemindersProcessingService : BackgroundService
    {
        private readonly IRemindersCache _cache;
        private readonly DiscordSocketClient _client;
        private readonly MiraminderService _reminderService;

        public RemindersProcessingService(
            IRemindersCache cache,
            DiscordSocketClient client,
            MiraminderService reminder)
        {
            _cache = cache;
            _client = client;
            _reminderService = reminder;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _cache.RefreshCacheAsync();

            int refreshCounter = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                var reminders = _cache.GetNextDueReminder();

                if (reminders.Any())
                {
                    await Task.WhenAll(
                        reminders.Select(async reminder =>
                        {
                            await SendReminderAsync(reminder);
                            if (reminder.IsRecurring)
                            {
                                await _reminderService.UpdateRecurringReminderAsync(reminder);
                            }
                        })
                    );
                    await _cache.RefreshCacheAsync();
                }

                refreshCounter++;
                if (refreshCounter >= 600)
                {
                    await _cache.RefreshCacheAsync();
                    refreshCounter = 0;
                }

                await Task.Delay(1000, CancellationToken.None);
            }
        }

        private async Task SendReminderAsync(Reminder reminder)
        {
            var user = await _reminderService.GetUserAsync(reminder.RecipientId);
            var recipient = await _client.Rest.GetUserAsync(user.DiscordId);
            var dm = await recipient.CreateDMChannelAsync();

            if (reminder.OwnerId == reminder.RecipientId)
            {
                await dm.SendMessageAsync($"Here's your reminder! The message attached to it is this: \"{reminder.Message}\"");
            }
            else
            {
                var owner = await _reminderService.GetUserAsync(reminder.OwnerId);
                var ownerDiscord = await _client.Rest.GetUserAsync(owner.DiscordId);
                var ownerDm = await ownerDiscord.CreateDMChannelAsync();
                try
                {
                    if (reminder.IsRecurring)
                    {
                        await dm.SendMessageAsync($"You have a reminder from {owner.UserName}! The message attached to this reminder is: \"{reminder.Message}\".\nThis reminder will go off again at {reminder.DateTime}. You can cancel reminders with ``/cancel.``");
                    }
                    else
                    {
                        await dm.SendMessageAsync($"You have a reminder from {owner.UserName}! The message attached to this reminder is: \"{reminder.Message}\"");
                    }
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
