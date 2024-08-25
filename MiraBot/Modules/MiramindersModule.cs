﻿using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using MiraBot.DataAccess;
using MiraBot.Miraminders;

namespace MiraBot.Modules
{
    public class MiramindersModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactive;
        private readonly MiraminderService _reminderService;
        private readonly ReminderHandler _handler;
        private readonly RemindersCache _cache;
        private readonly ModuleHelpers _helpers;
        public static int result = -1;
        public const int selectMenuLimit = 24;

        internal const int maxMessageLength = 250;

        public MiramindersModule(
            InteractiveService interactive,
            MiraminderService reminder,
            ReminderHandler handler,
            RemindersCache cache,
            ModuleHelpers moduleHelpers)
        {
            _interactive = interactive;
            _reminderService = reminder;
            _handler = handler;
            _cache = cache;
            _helpers = moduleHelpers;
        }


        [SlashCommand("remind", "Set a new reminder")]
        public async Task GetNewReminderAsync(string input)
        {
            await RespondAsync("Gimme a sec to look at this...");
            await _reminderService.EnsureUserExistsAsync(Context.User.Id, Context.User.Username);
            var owner = await _reminderService.GetUserByNameAsync(Context.User.Username);
            if (owner.Timezone is null)
            {
                await SaveUserTimezoneAsync(owner);
            }
            await _handler.ParseReminderAsync(input, Context.User.Id);
        }

        [SlashCommand("remindedit", "Edit an existing reminder.")]
        public async Task EditReminderAsync()
        {
            int index = 0;
            var user = await _reminderService.GetUserByDiscordId(Context.User.Id);
            var reminders = _cache.GetCacheContentsByUser(user.UserId);
            if (reminders.Count == 0)
            {
                await RespondAsync("It doesn't look like you have any active reminders!");
                return;
            }

            while (index != -1)
            {
                index = await GetIndexOfUserChoiceAsync(
                reminders.Select(r => r.Message).ToList(),
                "Select which reminder you'd like to edit",
                "Edit this reminder",
                Context);

                if (index == -1)
                {
                    break;
                }
            } 
        }

        [SlashCommand("remindcancel", "Cancel a reminder that either you own, or that someone sent to you.")]
        public async Task CancelReminderAsync()
        {
            int index = 0;
            var user = await _reminderService.GetUserByDiscordId(Context.User.Id);
            var reminders = _cache.GetCacheContentsByUser(user.UserId);
            if (reminders.Count == 0)
            {
                await RespondAsync("It doesn't look like you have any active reminders!");
                return;
            }

            while (index != -1)
            {
                index = await GetIndexOfUserChoiceAsync(
                                reminders.Select(r => r.Message).ToList(),
                                "Select which reminder you'd like to cancel",
                                "Cancel this reminder",
                                Context);

                if (index == -1)
                {
                    break;
                }

                await _reminderService.CancelReminderAsync(reminders[index]);
                await ReplyAsync("Okay, I cancelled that reminder for you. Anything else?");
                reminders.RemoveAt(index);
            }
        }

        [SlashCommand("remindfind", "Search your reminders that are saved by a keyword.")]
        public async Task FindReminderAsync(string word)
        {
            var user = await _reminderService.GetUserByDiscordId(Context.User.Id);
            var reminders = _cache.GetCacheContentsByUser(user.UserId);
            if (reminders.Count == 0)
            {
                await RespondAsync("It doesn't look like you have any active reminders!");
                return;
            }
            await RespondAsync("This hasn't been implemented yet!");
        }


        public async Task SaveUserTimezoneAsync(User owner)
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

                if (owner.UsesAmericanDateFormat is null)
                {
                    await ReplyAsync("How would you write out the date \"August 30th\"?");
                    var options = new List<string> { "8/30", "30/8" };
                    await GenerateSelectMenuAsync(options,
                        "How would you write out the date \"August 30th\"?",
                        string.Empty,
                        Context
                        );
                    var selection = result;
                    result = -1;
                    bool isAmerican;
                    if (selection == 0)
                    {
                        isAmerican = false;
                    }
                    else
                    {
                        isAmerican = true;
                    }

                    await _reminderService.AddDateFormatToUserAsync(owner.DiscordId, isAmerican);
                }

                await _reminderService.AddTimezoneToUserAsync(Context.User.Id, timezone);
                break;
            }
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

        public async Task<int> GetIndexOfUserChoiceAsync(List<string> messages,
            string placeholder,
            string? description,
            SocketInteractionContext ctx
            )
        {
            int selection;
            if (messages.Count <= selectMenuLimit)
            {
                await GenerateSelectMenuAsync(messages,
                    placeholder,
                    description,
                    ctx);
                selection = result;
                result = 0;
            }

            else
            {
                await SendLongMessageAsync(messages);
                selection = await _helpers.GetValidNumberAsync(0, messages.Count);
                selection--;
            }

            return selection;
        }


        public async Task SendLongMessageAsync(List<string> input)
        {
            var messages = _reminderService.SendLongMessage(input);

            foreach (var message in messages)
            {
                await ReplyAsync(message);
            }
        }

        public async Task GenerateSelectMenuAsync(
            List<string> inputs,
            string placeholder,
            string? optionDescription,
            SocketInteractionContext context,
            string defaultOption = "Nevermind",
            string defaultOptionDescription = "Abandon this action")
        {
            var optionId = 0;
            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder(placeholder)
                .AddOption(defaultOption, "nevermind", defaultOptionDescription);

            foreach (var input in inputs)
            {
                menuBuilder.AddOption(input, $"option-{optionId}", optionDescription);
                optionId++;
            }

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            var msg = await Context.Channel.SendMessageAsync(components: builder.Build());

            var menuResult = await _interactive.NextInteractionAsync(x => x.User.Username == Context.User.Username, timeout: TimeSpan.FromSeconds(120));

            if (menuResult.IsSuccess)
            {
                await menuResult.Value!.DeferAsync();
            }

            await msg.DeleteAsync();
        }
    }
}