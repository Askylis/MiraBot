﻿using Discord;
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
            await _helpers.SaveUserTimezoneAsync(user);
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
    }
}
