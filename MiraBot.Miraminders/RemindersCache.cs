using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Miraminders
{
    public class RemindersCache
    {
        private List<Reminder> cache;
        private List<Reminder>? remindersToSend = new();
        private readonly MiramindersRepository _repository;
        public RemindersCache(MiramindersRepository repository)
        {
            _repository = repository;
        }

        public async Task RefreshCache()
        {
            List<Reminder> allReminders;
            if (cache is not null)
            {
                cache.Clear();
            }
            allReminders = await _repository.GetRemindersAsync();
            var reminders = allReminders.Where(r => !r.IsCompleted).ToList();
            Console.WriteLine($"Number of active reminders: {reminders.Count}");
            cache = reminders.ToList();
        }

        public async Task<List<Reminder>?> GetActiveReminder()
        {
            if (cache is not null)
            {
                foreach (var reminder in cache)
                {
                    if (!reminder.IsCompleted && reminder.DateTime < DateTime.UtcNow)
                    {
                        remindersToSend.Add(reminder);
                        await _repository.MarkCompletedAsync(reminder);
                    }
                }
            }
            return remindersToSend;
        }

        public async Task AddReminderAsync(ulong ownerDiscordId, ulong recipientDiscordId, string message, DateTime dateTime)
        {
            var owner = await _repository.GetUserByDiscordIdAsync(ownerDiscordId);
            var recipient = ownerDiscordId != recipientDiscordId
                ? await _repository.GetUserByDiscordIdAsync(recipientDiscordId)
                : owner;

            var reminder = new Reminder
            {
                OwnerId = owner.UserId,
                RecipientId = recipient.UserId,
                Message = message,
                DateTime = dateTime,
                IsCompleted = false
            };

            cache.Add(reminder);
            await _repository.AddReminderAsync(reminder);
        }

    }
}