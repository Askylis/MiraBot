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

        public async Task AddReminderAsync(ulong ownerDiscordId, string recipientName, string message, DateTime dateTime)
        {
            var owner = await _repository.GetUserByDiscordId(ownerDiscordId);
            var recipient = await _repository.GetUserByNameAsync(recipientName);
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

        public async Task<List<Reminder>> GetRemindersByUserAsync(ulong discordId)
        {
            var user = await _repository.GetUserByDiscordId(discordId);
            var reminders = await _repository.GetRemindersAsync();
            return reminders.Where(r => r.OwnerId == user.UserId).ToList();
        }

        public static async Task DeleteReminderAsync()
        {

        }

        public async Task<string> UserTimeZone(string userName)
        {
            return await _repository.GetUserTimeZone(userName);
        }

        public async Task<User> GetUserAsync(int userId)
        {
            return await _repository.GetUserByUserId(userId);
        }

        public async Task<bool> UserExistsAsync(ulong discordId)
        {
            return await _repository.UserExistsAsync(discordId);
        }

        public async Task AddNewUserAsync(string userName, ulong discordId)
        {
            var user = new User
            {
                UserName = userName,
                DiscordId = discordId
            };
            await _repository.AddNewUserAsync(user);
        }
    }
}