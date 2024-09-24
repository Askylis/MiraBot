using Microsoft.Extensions.Hosting;
using MiraBot.Communication;
using MiraBot.DataAccess;
using MiraBot.Common;

namespace MiraBot.Miraminders
{
    public class RemindersProcessingService : BackgroundService
    {
        private readonly IRemindersCache _cache;
        private readonly MiraminderService _reminderService;
        private readonly UserCommunications _comms;
        private readonly ModuleHelpers _helpers;
        public RemindersProcessingService(
            IRemindersCache cache,
            MiraminderService reminder,
            UserCommunications comms,
            ModuleHelpers helpers)
        {
            _cache = cache;
            _reminderService = reminder;
            _comms = comms;
            _helpers = helpers;
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
            var recipient = await _helpers.GetUserByUserIdAsync(reminder.RecipientId);
            var owner = await _helpers.GetUserByUserIdAsync(reminder.OwnerId);

            if (reminder.OwnerId == reminder.RecipientId)
            {
                await _comms.SendMessageAsync(recipient, $"Here's your reminder! The message attached to it is this: \"{reminder.Message}\"");
            }
            else
            {
                try
                {
                    if (reminder.IsRecurring)
                    {
                        await _comms.SendMessageAsync(recipient, $"You have a reminder from {owner.UserName}! The message attached to this reminder is: \n\n**\"{reminder.Message}\"**.\nThis reminder will go off again at {reminder.DateTime}. You can cancel reminders with ``/cancel.``");
                    }
                    else
                    {
                        await _comms.SendMessageAsync(recipient, $"You have a reminder from {owner.UserName}! The message attached to this reminder is: \n\n**\"{reminder.Message}\"**");
                    }
                    await _comms.SendMessageAsync(owner, $"I just sent your reminder to {recipient.UserName}! Your reminder contained the following message: \n\n**\"{reminder.Message}\"**");
                }
                catch
                {
                    await _comms.SendMessageAsync(owner, $"Your reminder to {recipient.UserName} failed to send. This is likely due to their privacy settings.");
                }
            }
        }
    }
}
