﻿using Discord.Commands;
using Discord.Interactions;
using Fergun.Interactive;

namespace MiraBot.Modules
{
    public class ModuleHelpers : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactiveService;
        public ModuleHelpers(InteractiveService interactiveService)
        {
            _interactiveService = interactiveService;
        }

        public async Task<int> GetValidNumberAsync(int minNumber, int maxNumber, SocketInteractionContext context)
        {
            int userChoice = 0;
            bool isValid = false;

            while (!isValid)
            {
                var input = await _interactiveService.NextMessageAsync(x => x.Author.Id == context.User.Id && x.Channel.Id == context.Channel.Id,
            timeout: TimeSpan.FromMinutes(2));

                if (!input.IsSuccess)
                {
                    return 0;
                }
                isValid = int.TryParse(input.Value.Content, out userChoice);
                isValid = isValid && userChoice <= maxNumber && userChoice >= 0;
                if (!isValid)
                {
                    await ReplyAsync($"That doesn't seem to work. Please enter a number between {minNumber} and {maxNumber}.");
                }
            }

            return userChoice;
        }

        public async Task<int> GetValidNumberAsync(int minNumber, int maxNumber, SocketCommandContext context)
        {
            int userChoice = 0;
            bool isValid = false;

            while (!isValid)
            {
                var input = await _interactiveService.NextMessageAsync(x => x.Author.Id == context.User.Id && x.Channel.Id == context.Channel.Id,
            timeout: TimeSpan.FromMinutes(2));

                if (!input.IsSuccess)
                {
                    return 0;
                }
                isValid = int.TryParse(input.Value.Content, out userChoice);
                isValid = isValid && userChoice <= maxNumber && userChoice >= 0;
                if (!isValid)
                {
                    await ReplyAsync($"That doesn't seem to work. Please enter a number between {minNumber} and {maxNumber}.");
                }
            }

            return userChoice;
        }
    }
}
