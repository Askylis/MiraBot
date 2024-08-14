using MiraBot.DataAccess;

namespace MiraBot.Miraminders
{
    public interface IRemindersCache
    {
        Task RefreshCacheAsync();

        Reminder? GetNextDueReminder();
    }
}
