using Discord;
using Microsoft.Extensions.Logging;
using MiraBot.Common;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System.Text;

namespace MiraBot.Miraminders
{
    public class MiraminderService
    {
        private readonly IMiramindersRepository _remindersRepository;
        private readonly IUsersRepository _usersRepository;
        private readonly ILogger<MiraminderService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUsersCache _usersCache;

        public MiraminderService(IMiramindersRepository repository, ILogger<MiraminderService> logger, 
            IDateTimeProvider dateTimeProvider, IUsersCache usersCache, IUsersRepository usersRepository)
        {
            _remindersRepository = repository;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
            _usersCache = usersCache;
            _usersRepository = usersRepository;
        }

        public async Task<string?> GetUserTimeZoneAsync(ulong discordId)
        {
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            return user?.Timezone;
        }


        public async Task AddReminderAsync(Reminder reminder)
        {
            var owner = await _usersRepository.GetUserByUserIdAsync(reminder.OwnerId);
            await _remindersRepository.AddReminderAsync(reminder);
            _logger.LogDebug("Reminder added by {OwnerUserName}!", owner.UserName);
        }

        public async Task UpdateRecurringReminderAsync(Reminder reminder)
        {
            reminder.DateTime = reminder.DateTime.AddSeconds(reminder.InSeconds)
                .AddMinutes(reminder.InMinutes)
                .AddHours(reminder.InHours)
                .AddDays(reminder.InDays)
                .AddDays(reminder.InWeeks * 7)
                .AddMonths(reminder.InMonths)
                .AddYears(reminder.InYears);
            reminder.IsCompleted = false;
            await _remindersRepository.UpdateReminderAsync(reminder);
        }

        public async Task CancelReminderAsync(Reminder reminder)
        {
            reminder.IsRecurring = false;
            await _remindersRepository.RemoveReminderAsync(reminder.ReminderId);
        }


        public DateTime ConvertUserTimeToUtc(TimeOnly requestedTime, string userTimezoneId)
        {
            var userTimezone = TimeZoneInfo.FindSystemTimeZoneById(userTimezoneId);
            var userDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(_dateTimeProvider.UtcNow, userTimezone));
            var dateTime = userDate.ToDateTime(TimeOnly.MinValue).Add(requestedTime.ToTimeSpan());
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


        public async Task<List<string>> SendLongMessage(List<Reminder> reminders)
        {
            var response = new StringBuilder();
            var messages = new List<string>();
            int counter = 1;
            var owner = await _usersRepository.GetUserByUserIdAsync(reminders[0].OwnerId);

            foreach (var reminder in reminders)
            {
                string currentReminder;
                var userTime = TimeOnly.FromDateTime(ConvertUtcToUserTime(TimeOnly.FromDateTime(reminder.DateTime), owner.Timezone));
                var userDate = DateOnly.FromDateTime(reminder.DateTime);
                var userDateTime = userDate.ToDateTime(userTime);
                currentReminder = $"{counter}. **\"{reminder.Message}\"** is set for: **{userDateTime}**\n\n";

                if ((response.Length + currentReminder.Length) > 2000)
                {
                    messages.Add(response.ToString());
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