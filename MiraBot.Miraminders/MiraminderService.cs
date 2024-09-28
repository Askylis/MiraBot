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

        public MiraminderService(IMiramindersRepository repository, ILogger<MiraminderService> logger, 
            IUsersRepository usersRepository)
        {
            _remindersRepository = repository;
            _logger = logger;
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


        public DateTime ConvertUserDateTimeToUtc(DateTime requestedDateTime, string userTimezoneId)
        {
            var userTimezone = TimeZoneInfo.FindSystemTimeZoneById(userTimezoneId);
            return TimeZoneInfo.ConvertTimeToUtc(requestedDateTime, userTimezone);
        }


        public DateTime ConvertUtcDateTimeToUser(DateTime utcDateTime, string userTimezoneId)
        {
            var userTimezone = TimeZoneInfo.FindSystemTimeZoneById(userTimezoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, userTimezone);
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
                var userTime = TimeOnly.FromDateTime(ConvertUtcDateTimeToUser(reminder.DateTime, owner.Timezone));
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