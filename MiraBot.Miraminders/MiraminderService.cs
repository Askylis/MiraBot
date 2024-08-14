using Microsoft.Extensions.Logging;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Miraminders
{
    public class MiraminderService
    {
        private readonly IMiramindersRepository repository;
        private readonly ILogger<MiraminderService> _logger;

        public MiraminderService(IMiramindersRepository repository, ILogger<MiraminderService> logger)
        {
            this.repository = repository;
            _logger = logger;
        }

        public async Task<string?> GetUserTimeZoneAsync(ulong discordId)
        {
            var user = await repository.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            return user?.Timezone;
        }

        public async Task<User?> GetUserAsync(int userId)
        {
            return await repository.GetUserByUserIdAsync(userId)
                .ConfigureAwait(false);
        }

        public async Task<User> EnsureUserExistsAsync(ulong discordId, string username)
        {
            var user = await repository.GetUserByDiscordIdAsync(discordId);
            if (user is not null)
            {
                return user;
            }

            var newUser = new User { DiscordId = discordId, UserName = username };
            await repository.AddNewUserAsync(newUser);
            return newUser;
        }

        public async Task<User?> GetUserByNameAsync(string userName)
        {
            return await repository.GetUserByNameAsync(userName)
                .ConfigureAwait(false);
        }

        public static bool IsValidTimezone(string timezoneId)
        {
            return TimeZoneInfo
                .GetSystemTimeZones()
                .Any(t => t.Id.Equals(timezoneId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task AddReminderAsync(ulong ownerDiscordId, ulong recipientDiscordId, string message, DateTime dateTime)
        {
            var owner = await repository.GetUserByDiscordIdAsync(ownerDiscordId);
            var recipient = ownerDiscordId != recipientDiscordId
                ? await repository.GetUserByDiscordIdAsync(recipientDiscordId)
                : owner;

            if (owner is null || recipient is null)
            {
                throw new InvalidOperationException("Cannot create reminder - either owner or recipient not found.");
            }

            var reminder = new Reminder
            {
                OwnerId = owner.UserId,
                RecipientId = recipient.UserId,
                Message = message,
                DateTime = dateTime,
                IsCompleted = false
            };

            await repository.AddReminderAsync(reminder);

            _logger.LogDebug("Reminder added by {OwnerUserName}!", owner.UserName);
        }

        public async Task AddTimezoneToUserAsync(ulong discordId, string timezoneId)
        {
            var user = await repository.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            if (user is not null)
            {
                user.Timezone = timezoneId;
                await repository.ModifyUserAsync(user);
            }
        }

        public static string CreateTimezoneFile()
        {
            var path = Path.GetRandomFileName();
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            File.WriteAllLines(path, timeZones.Select(t => t.Id).ToArray());
            return path;
        }
    }
}
