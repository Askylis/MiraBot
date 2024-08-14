using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Miraminders
{
    public class RemindersCache : IRemindersCache
    {
        private readonly List<Reminder> _cache = new();

        private readonly IMiramindersRepository _repository;

        public RemindersCache(IMiramindersRepository repository)
        {
            this._repository = repository;
        }

        public async Task RefreshCacheAsync()
        {
            _cache.Clear();

            var reminders = await _repository.GetUpcomingRemindersAsync();
            Console.WriteLine($"Number of active reminders: {reminders.Count}");

            _cache.AddRange(reminders);
        }

        public Reminder? GetNextDueReminder()
        {
            var reminder = _cache
                .OrderBy(r => r.DateTime)
                .FirstOrDefault(r => r.DateTime >  DateTime.UtcNow);

            if (reminder is null )
            {
                return null;
            }

            _cache.Remove(reminder);
            _repository.MarkCompletedAsync(reminder.ReminderId);

            return reminder;
        }
    }
}