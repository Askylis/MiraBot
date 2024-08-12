using Discord.Interactions;
using Discord;
using Fergun.Interactive;
using MiraBot.DataAccess;
using MiraBot.Miraminders;

namespace MiraBot.Modules
{
    public class MiramindersModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactiveService;
        private readonly RemindersCache _cache;
        internal const int maxMessageLength = 250;

        public MiramindersModule(InteractiveService interactiveService, RemindersCache cache)
        {
            _interactiveService = interactiveService;
            _cache = cache;
        }

        [SlashCommand("remind", "Add a new reminder.")]
        public async Task AddReminder(int minutesFromNow, string reminderMessage)
        {
            await RespondAsync("Working on it...");
            //if (await _cache.UserTimeZone(Context.User.Username) is null)
            //{
            //    await ReplyAsync("You don't have a timezone registered!");
            //    await GetUserTimeZoneAsync();
            //    return;
            //}
            var reminderDateTime = DateTime.UtcNow + TimeSpan.FromMinutes(minutesFromNow);
            await _cache.AddReminderAsync(Context.User.Username, Context.User.Username, reminderMessage, reminderDateTime);
            await ReplyAsync($"Got it! Saved this reminder!");
        }

        public async Task GetUserTimeZoneAsync()
        {
            //need to find a way to get relevant timezones and display them to user via select menu
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            foreach (var timeZone in timeZones)
            {
                //Console.WriteLine(timeZone.ToString());
                Console.WriteLine(timeZone.Id.ToString());
            }
        }


        public async Task SetRecurringReminderAsync(int dayOfMonth)
        {
            var currentDate = DateTime.UtcNow;
        }
    }
}
