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
            // need to add check to make sure reminderMessage isn't over 250 characters long
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

            var recipientId = await GetRecipientAsync(recipientName);

            // Add reminder to DB
            await _reminderService.AddReminderAsync(Context.User.Id, recipientId, reminderMessage, reminderDateTime, false);
            await _cache.RefreshCacheAsync();

            await ReplyAsync("Got it! Saved this reminder!");
        }

        [SlashCommand("reminddaily", "Set a daily recurring reminder. Press tab to see all reminder options.")]
        public async Task SetDailyReminderAsync(string reminderMessage, string timeInput, string? recipientName = null)
        {
            // need to add check to make sure reminderMessage isn't over 250 characters long
            TimeOnly requestedTime;
            await RespondAsync("Working on it...");
            await _reminderService.EnsureUserExistsAsync(Context.User.Id, Context.User.Username);
            if (! TimeOnly.TryParse(timeInput, out requestedTime))
            {
                await ReplyAsync("Your time input isn't valid. Make sure that it's formatted as: HH:MM AM/PM. (AM/PM is optional, as you can use 24h time.)");
                return;
            }

            var utcTime = _reminderService.ConvertUserTimeToUtc(requestedTime, await GetUserTimezoneAsync());
            var recipientId = await GetRecipientAsync(recipientName);
            await _reminderService.AddReminderAsync(Context.User.Id, recipientId, reminderMessage, utcTime, true);
            await _cache.RefreshCacheAsync();

            await ReplyAsync($"Got it! Saved this reminder!");
        }


        [SlashCommand("test", "Test command for testing various functionality.")]
        public async Task TestCommandAsync(string timeInput)
        {
            // this is a test command with test code so I can check and make sure what I'm trying to do works properly
            TimeOnly time;
            await RespondAsync("Working on it...");
            await _reminderService.EnsureUserExistsAsync(Context.User.Id, Context.User.Username);
            if (!TimeOnly.TryParse(timeInput, out time))
            {
                await ReplyAsync("Your time input isn't valid. Make sure that it's formatted as: HH:MM AM/PM. (AM/PM is optional, as you can use 24h time.)");
                return;
            }
            var timezone = await GetUserTimezoneAsync();
            var userTime = _reminderService.ConvertUtcToUserTime(time, timezone);
            await ReplyAsync($"Your input: {time} UTC translated to {userTime} {timezone}");
        }

        public async Task<ulong> GetRecipientAsync(string? recipientName)
        {
            // If recipient is specified, look them up. Otherwise, use own ID
            var recipientId = Context.User.Id;
            if (recipientName is not null)
            {
                var recipient = await _reminderService.GetUserByNameAsync(recipientName);
                if (recipient is null)
                {
                    await ReplyAsync($"\"{recipientName}\" doesn't exist in the database! Make sure you spelled their username (not display name) correctly, and that this user has messaged me before!");
                    throw new InvalidOperationException($"Recipient {recipientName} not found.");
                }

                recipientId = recipient.DiscordId;
            }
            return recipientId;
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

        public async Task<string> GetUserTimezoneAsync()
        {
            var user = await _reminderService.EnsureUserExistsAsync(Context.User.Id, Context.User.Username);

            if (user.Timezone is null)
            {
                await ReplyAsync("Your time zone is not set.");
                await SaveUserTimezoneAsync();
            }

            TimeZoneInfo.FindSystemTimeZoneById(user.Timezone);
            return user.Timezone;
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
