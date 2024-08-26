using Microsoft.Extensions.Logging;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Miraminders
{
    public class RemindersCache : IRemindersCache
    {
        private static List<Reminder> _cache = new();
        private static List<Reminder> _toDelete = new();

        private readonly IMiramindersRepository _repository;
        private readonly ILogger<RemindersCache> _logger;

        public RemindersCache(IMiramindersRepository repository, ILogger<RemindersCache> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task RefreshCacheAsync()
        {
            _logger.LogInformation("Refreshing reminders cache...");
            await Task.WhenAll(_toDelete.Where(d => !d.IsRecurring).Select(reminder => _repository.MarkCompletedAsync(reminder.ReminderId)));
            _toDelete.Clear();
            _cache.Clear();
            var reminders = await _repository.GetUpcomingRemindersAsync();
            _logger.LogDebug("Number of active reminders: {remindersCount}", reminders.Count);
            _cache.AddRange(reminders);
        }

        public IEnumerable<Reminder> GetNextDueReminder()
        {
            var reminders = _cache
                .Where(r => r.DateTime < DateTime.UtcNow)
                .OrderBy(r => r.DateTime);
                
            if (reminders.Any())
            {
                foreach (var reminder in reminders)
                {
                    _toDelete.Add(reminder);
                    yield return reminder;
                }
            }
        }

        public List<Reminder> GetCacheContentsByUser(int userId)
        {
            var cache = _cache.Where(r => r.RecipientId == userId || r.OwnerId == userId).ToList();
            return cache;
        }
    }
}