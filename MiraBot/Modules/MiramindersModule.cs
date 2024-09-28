using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using MiraBot.Common;
using MiraBot.DataAccess;
using MiraBot.Miraminders;
using MiraBot.Permissions;

namespace MiraBot.Modules
{
    [NotBanned]
    public class MiramindersModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly MiraminderService _reminderService;
        private readonly RemindersCache _cache;
        private readonly ModuleHelpers _helpers;
        public const int selectMenuLimit = 24;
        private readonly IServiceProvider _serviceProvider;

        public MiramindersModule(
            MiraminderService reminder,
            RemindersCache cache,
            ModuleHelpers moduleHelpers,
            IServiceProvider serviceProvider)
        {
            _reminderService = reminder;
            _cache = cache;
            _helpers = moduleHelpers;
            _serviceProvider = serviceProvider;
        }


        [SlashCommand("remind", "Set a new reminder. Type your username or \"me\" to set for yourself.")]
        public async Task GetNewReminderAsync(string username, string input)
        {
            User? recipient;
            await RespondAsync("Gimme a sec to look at this...");
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }

            await _helpers.UpdateUsernameIfChangedAsync(Context);
            if (username.Equals("me", StringComparison.OrdinalIgnoreCase))
            {
                recipient = await _helpers.GetUserByDiscordIdAsync(Context.User.Id);
            }
            else
            {
                recipient = await _helpers.GetUserByNameAsync(username);
                if (recipient is null)
                {
                    await ReplyAsync($"I couldn't find anyone with the username \"{username}\". Please make sure you input the correct username, and try again!");
                    return;
                }
            }

            var owner = await _helpers.GetUserByNameAsync(Context.User.Username);
            if (owner.Timezone is null)
            {
                await _helpers.SaveUserTimezoneAsync(owner);
            }
            var handler = _serviceProvider.GetRequiredService<ReminderHandler>();
            var result = handler.ParseReminderAsync(input, owner, recipient);
            await ReplyAsync(result.Message);
            if (result.IsSuccess)
            {
                await _reminderService.AddReminderAsync(result.Reminder);
                await _cache.RefreshCacheAsync();
            }
        }


        [SlashCommand("remindcancel", "Cancel a reminder that either you own, or that someone sent to you.")]
        public async Task CancelReminderAsync()
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            await _helpers.UpdateUsernameIfChangedAsync(Context);
            int index = 0;
            var user = await _helpers.GetUserByDiscordIdAsync(Context.User.Id);
            var reminders = _cache.GetCacheContentsByUser(user.UserId);
            if (reminders.Count == 0)
            {
                await RespondAsync("It doesn't look like you have any active reminders!");
                return;
            }

            await RespondAsync("Gimme just a sec...");

            while (index != -1)
            {
                index = await _helpers.GetIndexOfUserChoiceAsync(
                                reminders,
                                "Select which reminder you'd like to cancel",
                                "Cancel this reminder",
                                Context,
                                "select-menu",
                                reminder => reminder.Message);

                if (index == -1)
                {
                    break;
                }

                await _reminderService.CancelReminderAsync(reminders[index]);
                await ReplyAsync("Okay, I cancelled that reminder for you. Anything else?");
                reminders.RemoveAt(index);
                await _cache.RefreshCacheAsync();
            }
        }


        [SlashCommand("remindlist", "List all of your active reminders.")]
        public async Task ListRemindersAsync()
        {
            await RespondAsync("Gimme just a sec!");
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            await _helpers.UpdateUsernameIfChangedAsync(Context);

            var user = await _helpers.GetUserByDiscordIdAsync(Context.User.Id);
            var reminders = _cache.GetCacheContentsByUser(user.UserId);
            if (reminders.Count == 0)
            {
                await ReplyAsync("You don't have any active reminders.");
                return;
            }
            await ModuleHelpers.SendLongMessageAsync(reminders.Select(r => r.Message).ToList(), Context);
        }


        [SlashCommand("remindfind", "Search your reminders that are saved by a keyword.")]
        public async Task FindReminderAsync(string word)
        {
            await RespondAsync("Lemme look this up...");
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            await _helpers.UpdateUsernameIfChangedAsync(Context);

            var user = await _helpers.GetUserByDiscordIdAsync(Context.User.Id);
            var reminders = _cache.GetCacheContentsByUser(user.UserId);
            if (reminders.Count == 0)
            {
                await ReplyAsync("It doesn't look like you have any active reminders!");
                return;
            }

            var matchingReminders = reminders.Where(r => r.Message.Contains(word)).ToList();

            if (matchingReminders.Count == 0)
            {
                await ReplyAsync($"I couldn't find a reminder that contained \"{word}\".");
                return;
            }
            await ReplyAsync($"I found {matchingReminders.Count} reminders!");
            await ModuleHelpers.SendLongMessageAsync(reminders.Select(r => r.Message).ToList(), Context);
        }
    }
}