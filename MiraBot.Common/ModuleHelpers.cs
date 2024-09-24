using Discord.Commands;
using Discord.Interactions;
using Fergun.Interactive;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Common
{
    public class ModuleHelpers : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactiveService;
        private readonly UsersRepository _usersRepository;
        public ModuleHelpers(InteractiveService interactiveService, UsersRepository usersRepository)
        {
            _interactiveService = interactiveService;
            _usersRepository = usersRepository;
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

        public async Task<bool> UserExistsAsync(ulong discordId)
        {
            return await _usersRepository.UserExistsAsync(discordId);
        }

        public async Task<User?> GetUserByNameAsync(string username)
        {
            return await _usersRepository.GetUserByNameAsync(username);
        }

        public async Task<User> GetUserByDiscordIdAsync(ulong discordId)
        {
            return await _usersRepository.GetUserByDiscordIdAsync(discordId);
        }

        public async Task<User?> GetUserByUserIdAsync(int userId)
        {
            return await _usersRepository.GetUserByUserIdAsync(userId)
                .ConfigureAwait(false);
        }
    }
}
