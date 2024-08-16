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
            _logger.LogInformation("Refreshing Reminders Cache");
            // I made _toDelete and marked as completed here, because originally each reminder that was to be sent in GetNextDueReminder
            // was being removed from cache and then marked completed, so they weren't actually ever being sent. 
            foreach (var reminder in _toDelete)
            {
                await _repository.MarkCompletedAsync(reminder.ReminderId);
            }
            _cache.Clear();
            var reminders = await _repository.GetUpcomingRemindersAsync();
            _logger.LogDebug("Number of active reminders: {remindersCount}", reminders.Count);

            _cache.AddRange(reminders);
            Console.WriteLine($"There should be {reminders.Count} reminders in cache.");
        }

        public IEnumerable<Reminder> GetNextDueReminder()
        {
            var reminders = _cache
                .Where(r => r.DateTime < DateTime.UtcNow)
                .OrderBy(r => r.DateTime);
                
            if (reminders.Any())
            {
                Console.WriteLine($"There are {reminders.Count()} reminders queued to be sent.");
                foreach (var reminder in reminders)
                {
                    _toDelete.Add(reminder);
                    yield return reminder;
                }
            }
        }
    }
}