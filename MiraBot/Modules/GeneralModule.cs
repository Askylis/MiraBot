using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.Options;
using MiraBot.Options;
using MiraBot.Permissions;

namespace MiraBot.Modules
{
    [NotBanned]
    public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CommandService _commandService;
        private readonly InteractionService _interactionService;
        private readonly IOptions<MiraOptions> _options;
        public GeneralModule(InteractionService interactionService, IOptions<MiraOptions> options, CommandService service)
        {
            _interactionService = interactionService;
            _options = options;
            _commandService = service;
        }

        [SlashCommand("help", "Displays all available Mira functionality and provides information on how to use it all.")]
        public async Task HelpAsync()
        {
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
    }
}
