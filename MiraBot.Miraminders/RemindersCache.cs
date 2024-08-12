using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System.Diagnostics;

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
            List<Reminder> allReminders = new();
            if (cache is not null)
            {
                cache.Clear();
            }
            allReminders = await _repository.GetRemindersAsync();
            var reminders = allReminders.Where(r => !r.IsCompleted).ToList();
            Console.WriteLine(reminders.Count);
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

        public async Task AddReminderAsync(string ownerName, string recipientName, string message, DateTime dateTime)
        {
            var reminder = new Reminder
            {
                OwnerName = ownerName,
                RecipientName = recipientName,
                Message = message,
                DateTime = dateTime,
                IsCompleted = false
            };
            cache.Add(reminder);
            await _repository.AddReminderAsync(reminder);
        }

        public async Task<List<Reminder>> GetRemindersByUserAsync(string userName)
        {
            var reminders = await _repository.GetRemindersAsync();
            return reminders.Where(r => r.OwnerName == userName).ToList();
        }

        public static async Task DeleteReminderAsync()
        {

        }

        public async Task<string> UserTimeZone(string userName)
        {
            return await _repository.GetUserTimeZone(userName);
        }
    }
}