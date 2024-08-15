namespace MiraBot.DataAccess.Repositories
{
    public interface IMiramindersRepository
    {
        Task AddReminderAsync(Reminder reminder);

        Task AddNewUserAsync(User user);

        Task<List<Reminder>> GetAllRemindersAsync();

        Task<List<Reminder>> GetUpcomingRemindersAsync();

        Task MarkCompletedAsync(int reminderId);

        Task<User?> GetUserByNameAsync(string userName);

        Task<User?> GetUserByDiscordIdAsync(ulong discordId);

        Task ModifyUserAsync(User user);

        Task<User?> GetUserByUserIdAsync(int userId);
        Task UpdateReminderAsync(Reminder reminder);
        Task DeleteReminderAsync(Reminder reminder);
    }
}
