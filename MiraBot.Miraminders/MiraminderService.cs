using Microsoft.Extensions.Logging;
using MiraBot.Common;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System.Text;

namespace MiraBot.Miraminders
{
    public class MiraminderService
    {
        private readonly IMiramindersRepository _repository;
        private readonly ILogger<MiraminderService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;

        public MiraminderService(IMiramindersRepository repository, ILogger<MiraminderService> logger, IDateTimeProvider dateTimeProvider)
        {
            _repository = repository;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<string?> GetUserTimeZoneAsync(ulong discordId)
        {
            var user = await _repository.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            return user?.Timezone;
        }

        public async Task<User?> GetUserAsync(int userId)
        {
            return await _repository.GetUserByUserIdAsync(userId)
                .ConfigureAwait(false);
        }

        public async Task<User> EnsureUserExistsAsync(ulong discordId, string username)
        {
            var user = await _repository.GetUserByDiscordIdAsync(discordId);
            if (user is not null)
            {
                return user;
            }

            var newUser = new User { DiscordId = discordId, UserName = username };
            await _repository.AddNewUserAsync(newUser);
            return newUser;
        }

        public async Task<User?> GetUserByNameAsync(string userName)
        {
            return await _repository.GetUserByNameAsync(userName)
                .ConfigureAwait(false);
        }

        public async Task<User?> GetUserByDiscordId(ulong discordId)
        {
            return await _repository.GetUserByDiscordIdAsync(discordId);
        }

        public static bool IsValidTimezone(string timezoneId)
        {
            return TimeZoneInfo
                .GetSystemTimeZones()
                .Any(t => t.Id.Equals(timezoneId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task AddReminderAsync(ulong ownerDiscordId, ulong recipientDiscordId, string message, DateTime dateTime, bool isRecurring)
        {
            var owner = await _repository.GetUserByDiscordIdAsync(ownerDiscordId);
            var recipient = ownerDiscordId != recipientDiscordId
                ? await _repository.GetUserByDiscordIdAsync(recipientDiscordId)
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
                IsCompleted = false,
                IsRecurring = isRecurring
            };

            await _repository.AddReminderAsync(reminder);
            _logger.LogDebug("Reminder added by {OwnerUserName}!", owner.UserName);
        }

        public async Task UpdateRecurringReminderAsync(Reminder reminder)
        {
            reminder.DateTime = reminder.DateTime.AddSeconds(reminder.InSeconds)
                .AddMinutes(reminder.InMinutes)
                .AddHours(reminder.InHours)
                .AddDays(reminder.InDays)
                .AddDays(reminder.InDays * 7)
                .AddMonths(reminder.InMonths)
                .AddYears(reminder.InYears);
            reminder.IsCompleted = false;
            await _repository.UpdateReminderAsync(reminder);
        }

        public DateTime ConvertUserTimeToUtc(TimeOnly requestedTime, string userTimezoneId)
        {
            var userTimezone = TimeZoneInfo.FindSystemTimeZoneById(userTimezoneId);
            var dateTime = _dateTimeProvider.Today.Add(requestedTime.ToTimeSpan());
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), userTimezone);
            return utcTime;
        }


        public DateTime ConvertUtcToUserTime(TimeOnly utcTime, string userTimezoneId)
        {
            var userTimezone = TimeZoneInfo.FindSystemTimeZoneById(userTimezoneId);
            var utcDateTime = _dateTimeProvider.Today.Add(utcTime.ToTimeSpan());
            var userTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Unspecified), userTimezone);
            return userTime;
        }


        public async Task AddTimezoneToUserAsync(ulong discordId, string timezoneId)
        {
            var user = await _repository.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            if (user is not null)
            {
                user.Timezone = timezoneId;
                await _repository.ModifyUserAsync(user);
            }
        }

        public async Task AddDateFormatToUserAsync(ulong discordId, bool isAmerican)
        {
            var user = await _repository.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            user.UsesAmericanDateFormat = isAmerican;
            await _repository.ModifyUserAsync(user);
        }

        public static string CreateTimezoneFile()
        {
            var fileName = Path.ChangeExtension(Path.GetRandomFileName(), ".txt");
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            File.WriteAllLines(fileName, timeZones.Select(t => t.Id).ToArray());
            return fileName;
        }

        public List<string> SendLongMessage(List<string> reminderMessages)
        {
            var response = new StringBuilder();
            var messages = new List<string>();
            int counter = 1;

            foreach (var message in reminderMessages)
            {
                string currentReminder;

                currentReminder = $"{counter}. {message}";

                if ((response.Length + currentReminder.Length) > 2000)
                {
                    message.Add(response.ToString());
                    response.Clear();
                }

                response.Append(currentReminder);
                counter++;
            }

            if (response.Length > 0)
            {
                messages.Add(response.ToString());
            }

            return messages;
        }
    }
}