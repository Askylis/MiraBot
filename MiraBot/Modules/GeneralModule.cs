using Discord;
using Discord.Commands;
using Discord.Interactions;
using Fergun.Interactive;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiraBot.Common;
using MiraBot.DataAccess;
using MiraBot.Permissions;

namespace MiraBot.Modules
{
    [NotBanned]
    public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CommandService _commandService;
        private readonly InteractionService _interactionService;
        private readonly InteractiveService _interactive;
        private readonly IOptions<MiraOptions> _options;
        private readonly ModuleHelpers _helpers;
        private const int maxDescriptionLength = 250;
        private const int maxReproduceLength = 500;
        public GeneralModule(InteractionService interactionService, IOptions<MiraOptions> options, CommandService service, ModuleHelpers helpers,
            InteractiveService interactive)
        {
            _interactionService = interactionService;
            _options = options;
            _commandService = service;
            _helpers = helpers;
            _interactive = interactive;
        }

        [SlashCommand("help", "Displays all available Mira functionality and provides information on how to use it all.")]
        public async Task HelpAsync()
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            await _helpers.UpdateUsernameIfChangedAsync(Context);
            // maybe update this later to provide more in-depth information about specific commands?
            var builder = new EmbedBuilder()
                .WithTitle("Available Commands")
                .WithColor(Color.Blue);

            foreach (var command in _interactionService.SlashCommands)
            {
                builder.AddField($"/{command.Name}", command.Description ?? "No description provided.");
            }

            if (Context.User.Id == _options.Value.DevId)
            {
                foreach (var command in _commandService.Commands)
                {
                    builder.AddField($"!{command.Name}", command.Summary ?? "No summary provided.");
                }
            }

            await RespondAsync(embed: builder.Build());
        }

        [SlashCommand("register", "Register yourself with Mira to access her functions.")]
        public async Task RegisterAsync()
        {
            if (await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("You've already registered with me, so you can't use this command again.");
                return;
            }
            var newUser = new User
            {
                DiscordId = Context.User.Id,
                UserName = Context.User.Username,
            };
            await _helpers.AddNewUserAsync(newUser);
            var user = await _helpers.GetUserByDiscordIdAsync(Context.User.Id);
            await SaveUserTimezoneAsync(user);
            await ReplyAsync("Got all the information I need! You can use `/help` to view all available commands.");
        }

        [SlashCommand("bugreport", "Report a bug to Mira's developer.")]
        public async Task ReportBugAsync(
            [Choice("Low: typos, poor performance, etc", "low"), Choice("Medium: incorrect values/responses, or data not saving correctly", "medium"), Choice("High: Mira completely stops functioning", "high")]
            [Discord.Interactions.Summary("severity", "Select how severe this bug is.")] string severity)
        {
            // log bugs elsewhere? like in console. or in bot channel in discord server?
            // also need to add a permission that blacklists people from submitting bug reports, but doesn't outright ban them from Mira?
            await RespondAsync("Gimme a sec to look at this...");
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            await _helpers.UpdateUsernameIfChangedAsync(Context);
            var user = await _helpers.GetUserByDiscordIdAsync(Context.User.Id);

            await RespondAsync($"Please describe the bug. Do not put steps to reproduce here. Responses cannot be longer than {maxDescriptionLength} characters long.");
            var description = await _helpers.GetResponseFromUserAsync(maxDescriptionLength, Context);
            if (description.IsNullOrEmpty())
            {
                return;
            }

            await ReplyAsync($"Please describe the steps to reproduce this bug. Responses cannot be longer than {maxReproduceLength} characters long.");
            var reproduce = await _helpers.GetResponseFromUserAsync(maxReproduceLength, Context);
            if (reproduce.IsNullOrEmpty())
            {
                return;
            }

            var report = new Bug
            {
                Description = description,
                HowToReproduce = reproduce,
                Severity = severity,
                DateTime = DateTime.Now
            };

            await _helpers.SaveBugAsync(report, Context.User.Id);
            var bug = await _helpers.GetNewestBugAsync();
            await ReplyAsync("This bug report has been sent to the developer. Thank you!");
            var dm = await Context.Client.GetUserAsync(_options.Value.DevId);
            await dm.SendMessageAsync($"New **{severity} severity** bug report from **{user.UserName}**!\n**Bug description:**\n\n\"{description}\"\n\n**Steps to reproduce:**\n\n\"{reproduce}\".");
            await dm.SendMessageAsync($"This bug has been saved with **bug ID {bug.Id}**.");
        }

        [SlashCommand("blacklist", "Block a user from being able to interact with you through Mira.")]
        public async Task BlacklistAsync(string username)
        {
            var user = await _helpers.GetUserByNameAsync(username);
            if (user == null)
            {
                await RespondAsync("Could not find a user with that username. You may have mistyped the name, or the recipient has not registered with me yet.");
                return;
            }
            await _helpers.BlacklistUserAsync(Context.User.Id, user.UserId);
            await RespondAsync($"{user.UserName} has been blacklisted. They will not be able to send you anything through me anymore.");
        }

        [SlashCommand("whitelist", "Allow a user to interact with you through Mira.")]
        public async Task WhitelistAsync(string username)
        {
            var user = await _helpers.GetUserByNameAsync(username);
            if (user == null)
            {
                await RespondAsync("Could not find a user with that username. You may have mistyped the name, or the recipient has not registered with me yet.");
                return;
            }
            await _helpers.WhitelistUserAsync(Context.User.Id, user.UserId);
            await RespondAsync($"{user.UserName} has been whitelisted. They will now be able to send you things through me.");
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

                if (!IsValidTimezone(timezone))
                {
                    await ReplyAsync("The timezone you entered isn't valid! Make sure to copy your timezone exactly as it's listed in the file I sent.");
                    continue;
                }

                if (owner.UsesAmericanDateFormat is null)
                {
                    await ReplyAsync("How would you write out the date \"August 30th\"?");
                    var options = new List<string> { "8/30", "30/8" };
                    await _helpers.GenerateSelectMenuAsync(options,
                        "How would you write out the date \"August 30th\"?",
                        "select-menu",
                        "Select this option",
                        Context
                        );
                    var selection = ModuleHelpers.result;
                    ModuleHelpers.result = -1;
                    bool isAmerican = (selection == 0);

                    await AddDateFormatToUserAsync(owner.DiscordId, isAmerican);
                }

                await AddTimezoneToUserAsync(Context.User.Id, timezone);
                break;
            }
        }

        public static bool IsValidTimezone(string timezoneId)
        {
            return TimeZoneInfo
                .GetSystemTimeZones()
                .Any(t => t.Id.Equals(timezoneId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task SendTimezoneFileAsync()
        {
            string fileName;
            try
            {
                fileName = CreateTimezoneFile();
            }
            catch (Exception ex)
            {
                await ReplyAsync("I was unable to create the timezone file. Please seek assistance from the developer.");
                return;
            }

            await FollowupWithFileAsync(fileName);
        }

        public async Task AddTimezoneToUserAsync(ulong discordId, string timezoneId)
        {
            var user = await _helpers.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            if (user is not null)
            {
                user.Timezone = timezoneId;
                await _helpers.ModifyUserAsync(user);
            }
        }

        public async Task AddDateFormatToUserAsync(ulong discordId, bool isAmerican)
        {
            var user = await _helpers.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            user.UsesAmericanDateFormat = isAmerican;
            await _helpers.ModifyUserAsync(user);
        }

        public static string CreateTimezoneFile()
        {
            var fileName = Path.ChangeExtension(Path.GetRandomFileName(), ".txt");
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            File.WriteAllLines(fileName, timeZones.Select(t => t.Id).ToArray());
            return fileName;
        }
    }
}
