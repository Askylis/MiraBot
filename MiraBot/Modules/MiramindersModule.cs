using Discord;
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
        public static int result = -1;

        internal const int maxMessageLength = 250;

        public MiramindersModule(
            InteractiveService interactive, 
            MiraminderService reminder,
            ReminderHandler handler)
        {
            _interactive = interactive;
            _reminderService = reminder;
            _handler = handler;
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
            await RespondAsync("This hasn't been implemented yet!");
        }

        [SlashCommand("remindcancel", "Cancel a reminder that either you own, or that someone sent to you.")]
        public async Task CancelReminderAsync()
        {
            await RespondAsync("This hasn't been implemented yet!");
        }

        [SlashCommand("remindfind", "Search your reminders that are saved by a keyword.")]
        public async Task FindReminderAsync(string word)
        {
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
                    await GenerateSelectMenuAsync();
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

        public async Task GenerateSelectMenuAsync()
        {
            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Select a format.")
                .WithCustomId("select-menu")
                .AddOption("8/30", "1")
                .AddOption("30/8", "0");

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
