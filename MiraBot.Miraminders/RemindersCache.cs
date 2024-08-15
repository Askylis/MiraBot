using Microsoft.Extensions.Logging;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Miraminders
{
    public class RemindersCache : IRemindersCache
    {
        private readonly List<Reminder> _cache = new();

        private readonly IMiramindersRepository _repository;
        private readonly ILogger<RemindersCache> _logger;

        public RemindersCache(IMiramindersRepository repository, ILogger<RemindersCache> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task RefreshCacheAsync()
        {
            _logger.LogInformation("Refreshing Reminders Cache");
            _cache.Clear();

            var reminders = await _repository.GetUpcomingRemindersAsync();
            _logger.LogDebug("Number of active reminders: {remindersCount}", reminders.Count);

            _cache.AddRange(reminders);
        }

        public Reminder? GetNextDueReminder()
        {
            var reminder = _cache
                .OrderBy(r => r.DateTime)
                .FirstOrDefault(r => r.DateTime <  DateTime.UtcNow);

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