using Discord.Interactions;
using Discord;
using Fergun.Interactive;
using MiraBot.Miraminders;
using System.Reflection.Metadata.Ecma335;

namespace MiraBot.Modules
{
    public class MiramindersModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactive;
        private readonly RemindersCache _cache;
        private readonly Miraminder _reminder;
        internal const int maxMessageLength = 250;

        public MiramindersModule(InteractiveService interactive, RemindersCache cache, Miraminder reminder)
        {
            _interactive = interactive;
            _cache = cache;
            _reminder = reminder;
        }


        [SlashCommand("remind", "Set a reminder. Recipient name is optional. Press tab to see all reminder options.")]
        public async Task SendReminderAsync(string reminderMessage, string? recipientName = null, int days = 0, int hours = 0, int minutes = 0)
        {
            await RespondAsync("Working on it...");
            if (!await _reminder.UserExistsAsync(Context.User.Id))
            {
                await _reminder.AddNewUserAsync(Context.User.Username, Context.User.Id);
            }
            if (await _reminder.GetUserTimezoneAsync(Context.User.Id) is null)
            {
                await ReplyAsync("I don't have a timezone saved for you! Let's fix that.");
                await SaveUserTimezoneAsync();
            }
            var reminderDateTime = DateTime.UtcNow + TimeSpan.FromDays(days) + TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
            if (recipientName is null)
            {
                await ReplyAsync("Got it! Saved this reminder!");
                await _cache.AddReminderAsync(Context.User.Id, Context.User.Id, reminderMessage, reminderDateTime);
            }
            else
            {
                var recipient = await _reminder.GetUserByNameAsync(recipientName);
                if (recipient is null)
                {
                    await ReplyAsync($"\"{recipientName}\" doesn't exist in my database! Make sure you spelled their username right (not their display name), and make sure they've talked to me before!");
                    return;
                }
                await _cache.AddReminderAsync(Context.User.Id, recipient.DiscordId, reminderMessage, reminderDateTime);
                await ReplyAsync($"Saved this reminder! I'll make sure to send it to {recipientName}.");
            }
        }

        [SlashCommand("reminddaily", "Set a daily recurring reminder. Press tab to see all reminder options.")]
        public async Task SetRecurringDailyReminderAsync(string reminderMessage, string time, string? recipientName = null)
        {
            //need to convert time variable to a TimeOnly object
            await RespondAsync("Working on it...");
            if (!await _reminder.UserExistsAsync(Context.User.Id))
            {
                await _reminder.AddNewUserAsync(Context.User.Username, Context.User.Id);
            }
            var userTimezone = await GetUserTimezoneAsync();
        }


        public async Task SaveUserTimezoneAsync()
        {
            bool isValid = false;
            await SendTimezoneFileAsync();
            while (!isValid)
            {
                await ReplyAsync("Copy your timezone exactly how it's listed in this file, and send it to me so I can register your timezone!");
                var input = await _interactive.NextMessageAsync(x => x.Author.Id == Context.User.Id && x.Channel.Id == Context.Channel.Id,
                timeout: TimeSpan.FromMinutes(2));
                var timezone = input.Value.Content.Trim();
                if (!input.IsSuccess)
                {
                    return;
                }
                if (!_reminder.IsValidTimezone(timezone))
                {
                    await ReplyAsync("The timezone you entered isn't valid! Make sure to copy your timezone exactly as it's listed in the file I sent.");
                }
                else
                {
                    await ReplyAsync($"{timezone} is valid!");
                    await _reminder.AddTimezoneToUserAsync(Context.User.Id, timezone);
                    isValid = true;
                }
            }
        }

        [SlashCommand("test", "quick timezone test")]
        public async Task<string> GetUserTimezoneAsync()
        {
            var timezone = await _reminder.GetUserTimezoneAsync(Context.User.Id);
            TimeZoneInfo.FindSystemTimeZoneById(timezone);
            await ReplyAsync($"Your timezone is {timezone}!");
            return timezone;
        }


        public async Task SendTimezoneFileAsync()
        {
            var fileName = "timezones.txt";
            var filePath = _reminder.GetOutputPath(fileName);
            _reminder.BuildTimezoneFile(filePath);
            await FollowupWithFileAsync(filePath);
        }
    }
}
