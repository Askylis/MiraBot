namespace MiraBot.DataAccess.Repositories
{
    public interface IMiramindersRepository
    {
        Task AddReminderAsync(Reminder reminder);

        Task<List<Reminder>> GetAllRemindersAsync();

        Task<List<Reminder>> GetUpcomingRemindersAsync();

        Task RemoveReminderAsync(int reminderId);

        Task UpdateReminderAsync(Reminder reminder);
    }
}
