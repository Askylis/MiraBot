using Discord.Interactions;
using Discord;
using Fergun.Interactive;
using MiraBot.Miraminders;

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


        [SlashCommand("remindme", "Set a reminder for yourself. ")]
        public async Task AddReminderAsync(string reminderMessage, int days = 0, int hours = 0, int minutes = 0)
        {
            await RespondAsync("Working on it...");
            if (!await _reminder.UserExistsAsync(Context.User.Id))
            {
                await _reminder.AddNewUserAsync(Context.User.Username, Context.User.Id);
            }
            if (await _reminder.UserTimeZone(Context.User.Id) is null)
            {
                await ReplyAsync("You don't have a timezone registered!");
                await GetUserTimeZoneAsync();
            }
            var reminderDateTime = DateTime.UtcNow + TimeSpan.FromDays(days) + TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
            await _cache.AddReminderAsync(Context.User.Id, Context.User.Id, reminderMessage, reminderDateTime);
            await ReplyAsync($"Got it! Saved this reminder!");
        }

        [SlashCommand("sendreminder", "Send a reminder to another previously registered user.")]
        public async Task SendReminderToOtherUserAsync(string reminderMessage, string recipientName, int days = 0, int hours = 0, int minutes = 0)
        {
            await RespondAsync("Working on it...");
            if (!await _reminder.UserExistsAsync(Context.User.Id))
            {
                await _reminder.AddNewUserAsync(Context.User.Username, Context.User.Id);
            }
            var recipient = await _reminder.GetUserByNameAsync(recipientName);
            if (recipient is null)
            {
                await ReplyAsync($"\"{recipientName}\" doesn't exist in my database! Make sure you spelled their username right (not their display name), and make sure they've talked to me before!");
            }
            else
            {
                var reminderDateTime = DateTime.UtcNow + TimeSpan.FromDays(days) + TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
                await _cache.AddReminderAsync(Context.User.Id, recipient.DiscordId, reminderMessage, reminderDateTime);
                await ReplyAsync($"Saved this reminder! I'll make sure to send it to {recipientName}.");
            }
        }

        public async Task GetUserTimeZoneAsync()
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
                    await _reminder.AddTimezoneToUser(Context.User.Id, timezone);
                    isValid = true;
                }
            }
        }



        public async Task SetRecurringReminderAsync(int dayOfMonth)
        {
            var currentDate = DateTime.UtcNow;
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
