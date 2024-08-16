using MiraBot.DataAccess;

namespace MiraBot.Miraminders
{
    public interface IRemindersCache
    {
        Task RefreshCacheAsync();

        IEnumerable<Reminder> GetNextDueReminder();
    }
}
