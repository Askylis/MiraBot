using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.Options;
using MiraBot.Common;
using MiraBot.Options;
using MiraBot.Permissions;
using MiraBot.DataAccess;
using Microsoft.IdentityModel.Tokens;

namespace MiraBot.Modules
{
    [NotBanned]
    public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CommandService _commandService;
        private readonly InteractionService _interactionService;
        private readonly IOptions<MiraOptions> _options;
        private readonly ModuleHelpers _helpers;
        private const int maxDescriptionLength = 250;
        private const int maxReproduceLength = 500;
        public GeneralModule(InteractionService interactionService, IOptions<MiraOptions> options, CommandService service, ModuleHelpers helpers)
        {
            _interactionService = interactionService;
            _options = options;
            _commandService = service;
            _helpers = helpers;
        }

        [SlashCommand("help", "Displays all available Mira functionality and provides information on how to use it all.")]
        public async Task HelpAsync()
        {
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

            // get timezone
            // get date format (mm/dd vs dd/mm)
        }

        [SlashCommand("bugreport", "Report a bug to Mira's developer.")]
        public async Task ReportBugAsync(
            [Choice("Low: typos, poor performance, etc", "low"), Choice("Medium: incorrect values or responses", "medium"), Choice("High: this bug completely broke Mira", "high")]
            [Discord.Interactions.Summary("severity", "Select how severe this bug is.")] string severity)
        {
            var user = await _helpers.GetUserByDiscordIdAsync(Context.User.Id);

            await RespondAsync($"Please describe the bug. Do not put steps to reproduce here. Responses cannot be longer than {maxDescriptionLength} characters long.");
            var description = await _helpers.GetResponseFromUserAsync(maxDescriptionLength, Context);
            if (description.IsNullOrEmpty())
            {
                return;
            }

            await RespondAsync($"Please describe the steps to reproduce this bug. Responses cannot be longer than {maxReproduceLength} characters long.");
            var reproduce = await _helpers.GetResponseFromUserAsync(maxReproduceLength, Context);
            if (reproduce.IsNullOrEmpty())
            {
                return;
            }

            var report = new Bug
            {
                User = user,
                Description = description,
                HowToReproduce = reproduce,
                Severity = severity,
                DateTime = DateTime.Now
            };

            await _helpers.SaveBugAsync(report);
            await ReplyAsync("This bug report has been sent to the developer. Thank you!");
            var dm = await Context.Client.GetUserAsync(_options.Value.DevId);
            await dm.SendMessageAsync($"New bug report from {user.UserName}!");
        }
    }
}
