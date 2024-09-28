namespace MiraBot.DataAccess.Repositories
{
    public interface IUsersRepository
    {
        Task AddNewUserAsync(User user);
        Task<User?> GetUserByNameAsync(string userName);
        Task<User?> GetUserByDiscordIdAsync(ulong discordId);
        Task<User?> GetUserByUserIdAsync(int userId);
        Task ModifyUserAsync(User user);
        Task UpdatePermissionsAsync(User user, Permission permission);
        Task<bool> UserExistsAsync(ulong discordId);
        Task BlacklistUserAsync(ulong recipientDiscordId, int senderId);
        Task WhitelistUserAsync(ulong recipientDiscordId, int senderId);
        Task<bool> UserIsBlacklistedAsync(ulong senderDiscordId, int recipientId);
        Task<bool> UserIsWhitelistedAsync(ulong senderDiscordId, int recipientId);
    }
}
