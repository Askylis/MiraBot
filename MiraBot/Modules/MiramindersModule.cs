using Discord.Interactions;
using Fergun.Interactive;
using MiraBot.Miraminders;

namespace MiraBot.Modules
{
    public class MiramindersModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactive;
        private readonly IRemindersCache _cache;
        private readonly MiraminderService _reminderService;

        internal const int maxMessageLength = 250;

        public MiramindersModule(
            InteractiveService interactive, 
            RemindersCache cache, 
            MiraminderService reminder)
        {
            _interactive = interactive;
            _cache = cache;
            _reminderService = reminder;
        }

        [SlashCommand("remind", "Set a reminder. Recipient name is optional. Press tab to see all reminder options.")]
        public async Task SendReminderAsync(string reminderMessage, string? recipientName = null, int days = 0, int hours = 0, int minutes = 0)
        {
            await RespondAsync("Working on it...");

            var user = await _reminderService.EnsureUserExistsAsync(Context.User.Id, Context.User.Username);

            // Ensure user has a timezone set
            if (user.Timezone is null)
            {
                await ReplyAsync("I don't have a timezone saved for you! Let's fix that.");
                await SaveUserTimezoneAsync();
            }

            // Calculate date and time
            var reminderDateTime = DateTime.UtcNow.Add(new TimeSpan(days, hours, minutes, 0));

            // If recipient is specified, look them up. Otherwise, use own ID
            var recipientId = Context.User.Id;
            if (recipientName is not null)
            {
                var recipient = await _reminderService.GetUserByNameAsync(recipientName);
                if (recipient is null)
                {
                    await ReplyAsync($"\"{recipientName}\" doesn't exist in my database! Make sure you spelled their username right (not their display name), and make sure they've talked to me before!");
                    return;
                }

                recipientId = recipient.DiscordId;
            }

            // Add reminder to DB
            await _reminderService.AddReminderAsync(Context.User.Id, recipientId, reminderMessage, reminderDateTime);
            await _cache.RefreshCacheAsync();

            await ReplyAsync("Got it! Saved this reminder!");
        }

        [SlashCommand("reminddaily", "Set a daily recurring reminder. Press tab to see all reminder options.")]
        public async Task SetRecurringDailyReminderAsync(string reminderMessage, string time, string? recipientName = null)
        {
            //need to convert time variable to a TimeOnly object
            await RespondAsync("Working on it...");
            await _reminderService.EnsureUserExistsAsync(Context.User.Id, Context.User.Username);

            // Commented this out as you shouldn't be calling a command method yourself
            // but I'm not sure where you were going with this
            // var userTimezone = await GetUserTimezoneAsync();
        }


        public async Task SaveUserTimezoneAsync()
        {
            await SendTimezoneFileAsync();

            while (true)
            {
                await ReplyAsync("Copy your timezone exactly how it's listed in this file, and send it to me so I can register your timezone!");

                var input = await _interactive.NextMessageAsync(
                    x => x.Author.Id == Context.User.Id && x.Channel.Id == Context.Channel.Id,
                    timeout: TimeSpan.FromMinutes(2));

                if (!input.IsSuccess)
                {
                    continue;
                }

                var timezone = input.Value.Content.Trim();

                if (!MiraminderService.IsValidTimezone(timezone))
                {
                    await ReplyAsync("The timezone you entered isn't valid! Make sure to copy your timezone exactly as it's listed in the file I sent.");
                    continue;
                }

                await ReplyAsync($"{timezone} is valid!");
                await _reminderService.AddTimezoneToUserAsync(Context.User.Id, timezone);
                break;
            }
        }

        [SlashCommand("test", "quick timezone test")]
        public async Task GetUserTimezoneAsync()
        {
            var user = await _reminderService.EnsureUserExistsAsync(Context.User.Id, Context.User.Username);

            if (user.Timezone is null)
            {
                await ReplyAsync("Your time zone is not set.");
                return;
            }

            TimeZoneInfo.FindSystemTimeZoneById(user.Timezone);
            await ReplyAsync($"Your timezone is {user.Timezone}!");
        }


        public async Task SendTimezoneFileAsync()
        {
            string fileName;
            try
            {
                fileName = MiraminderService.CreateTimezoneFile();
            }
            catch (Exception ex)
            {
                // TODO: Log exception
                await ReplyAsync("I was unable to create the timezone file. Please seek assistance from the developer.");
                return;
            }

            await FollowupWithFileAsync(fileName);
        }
    }
}
