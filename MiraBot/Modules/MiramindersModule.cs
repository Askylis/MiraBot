using Discord.Interactions;
using Discord;
using Fergun.Interactive;
using MiraBot.DataAccess;
using MiraBot.Miraminders;
using MiraBot.GroceryAssistance;

namespace MiraBot.Modules
{
    public class MiramindersModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactiveService;
        private readonly RemindersCache _cache;
        private readonly Miraminder _reminder;
        internal const int maxMessageLength = 250;

        public MiramindersModule(InteractiveService interactiveService, RemindersCache cache, Miraminder reminder)
        {
            _interactiveService = interactiveService;
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
                return;
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
            await SendTimezoneFileAsync();
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
