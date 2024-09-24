﻿using Discord;
using Discord.Commands;
using Discord.Interactions;
using Fergun.Interactive;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Common
{
    public class ModuleHelpers : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactive;
        private readonly UsersRepository _usersRepository;
        public static int result = -1;
        internal const int selectMenuLimit = 24;
        public ModuleHelpers(InteractiveService interactiveService, UsersRepository usersRepository)
        {
            _interactive = interactiveService;
            _usersRepository = usersRepository;
        }

        public async Task<int> GetValidNumberAsync(int minNumber, int maxNumber, SocketInteractionContext context)
        {
            int userChoice = 0;
            bool isValid = false;

            while (!isValid)
            {
                var input = await _interactive.NextMessageAsync(x => x.Author.Id == context.User.Id && x.Channel.Id == context.Channel.Id,
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
                var input = await _interactive.NextMessageAsync(x => x.Author.Id == context.User.Id && x.Channel.Id == context.Channel.Id,
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
                    await GenerateSelectMenuAsync(options,
                        "How would you write out the date \"August 30th\"?",
                        "select-menu",
                        "Select this option",
                        Context
                        );
                    var selection = result;
                    result = -1;
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


        public async Task<int> GetIndexOfUserChoiceAsync<T>(
            List<T> items,
            string placeholder,
            string? description,
            SocketInteractionContext ctx,
            string customId,
            Func<T, string> messageSelector
            )
        {
            int selection;
            if (items.Count <= selectMenuLimit)
            {
                await GenerateSelectMenuAsync(items.Select(messageSelector).ToList(),
                    placeholder,
                    customId,
                    description,
                    ctx);
                selection = result;
                result = 0;
            }

            else
            {
                await SendLongMessageAsync(items.Select(messageSelector).ToList());
                selection = await GetValidNumberAsync(0, items.Count, Context);
                selection--;
            }

            return selection;
        }


        public async Task GenerateSelectMenuAsync(
            List<string> inputs,
            string placeholder,
            string customId,
            string? optionDescription,
            SocketInteractionContext context,
            string defaultOption = "Nevermind",
            string defaultOptionDescription = "Abandon this action")
        {
            var optionId = 0;
            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder(placeholder)
                .WithCustomId(customId)
                .AddOption(defaultOption, "nevermind", defaultOptionDescription);

            foreach (var input in inputs)
            {
                menuBuilder.AddOption(input, $"option-{optionId}", optionDescription);
                optionId++;
            }

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            var msg = await context.Channel.SendMessageAsync(components: builder.Build());

            var menuResult = await _interactive.NextInteractionAsync(x => x.User.Username == context.User.Username, timeout: TimeSpan.FromSeconds(120));

            if (menuResult.IsSuccess)
            {
                await menuResult.Value!.DeferAsync();
            }

            await msg.DeleteAsync();
        }

        public async Task SendLongMessageAsync(List<string> messages)
        {
            foreach (var message in messages)
            {
                await ReplyAsync(message);
            }
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

        public async Task<bool> IsValidNumberAsync(int number, int maxNumber,  ulong discordId, bool allowZero)
        {
            if ((!allowZero && number <= 0) || (allowZero && number < 0))
            {
                return false;
            }
            return number <= maxNumber;
        }

        public async Task AddTimezoneToUserAsync(ulong discordId, string timezoneId)
        {
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            if (user is not null)
            {
                user.Timezone = timezoneId;
                await _usersRepository.ModifyUserAsync(user);
            }
        }

        public async Task AddDateFormatToUserAsync(ulong discordId, bool isAmerican)
        {
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId)
                .ConfigureAwait(false);

            user.UsesAmericanDateFormat = isAmerican;
            await _usersRepository.ModifyUserAsync(user);
        }

        public static string CreateTimezoneFile()
        {
            var fileName = Path.ChangeExtension(Path.GetRandomFileName(), ".txt");
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            File.WriteAllLines(fileName, timeZones.Select(t => t.Id).ToArray());
            return fileName;
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
