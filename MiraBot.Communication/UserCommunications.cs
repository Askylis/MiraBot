using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using MiraBot.DataAccess;
using MiraBot.Common;
using Fergun.Interactive;

namespace MiraBot.Communication
{
    public class UserCommunications : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ModuleHelpers _helpers;
        private readonly InteractiveService _interactive;
        public UserCommunications(DiscordSocketClient client, ModuleHelpers helpers, InteractiveService interactive)
        {
            _client = client;
            _helpers = helpers;
            _interactive = interactive;
        }

        // Will need to update this class to handle blacklisting/whitelisting and such. 
        public async Task SendMessageAsync(User recipient, string content)
        {
            var discordRecipient = await _client.Rest.GetUserAsync(recipient.DiscordId);
            var dm = await discordRecipient.CreateDMChannelAsync();

            await dm.SendMessageAsync(content);
        }

        public async Task SendRecipeAsync(User recipient, User owner, Meal meal)
        {
            var discordRecipient = await _client.Rest.GetUserAsync(recipient.DiscordId);
            var dm = await discordRecipient.CreateDMChannelAsync();
            await dm.SendMessageAsync($"{owner.UserName} sent you a recipe for \"{meal.Name}\"! Here's the recipe for it.");
            await SendRecipeFileAsync(meal.Recipe, dm);
            await AddButtonsAsync(dm, meal.MealId, "Do you want to save this recipe?");
        }

        public async Task GetUserConsentAsync(User recipient, User sender, string objectType)
        {
            var discordRecipient = await _client.Rest.GetUserAsync(recipient.DiscordId);
            var dm = await discordRecipient.CreateDMChannelAsync();
            await dm.SendMessageAsync($"{sender.UserName} is trying to send you a {objectType}!");
            var consent = await UserWantsAsync("Do you want to allow them to communicate with you through me?", recipient.DiscordId);
            if (consent.Value)
            {
                await _helpers.WhitelistUserAsync(recipient.DiscordId, sender.UserId);
                await dm.SendMessageAsync($"{sender.UserName} has been whitelisted! You can change this at any time with `/blacklist` or `/whitelist`.");
            }
            else if (!consent.Value)
            {
                await _helpers.BlacklistUserAsync(recipient.DiscordId, sender.UserId);
                await dm.SendMessageAsync($"{sender.UserName} has been blacklisted! You can change this at any time with `/blacklist` or `/whitelist`.");
            }
            else
            {
                await dm.SendMessageAsync("I didn't get a reply.");
            }
        }

        public async Task<bool?> UserWantsAsync(string question, ulong recipientDiscordId)
        {
            int counter = 0;
            int maxAttempts = 3;
            var discordRecipient = await _client.Rest.GetUserAsync(recipientDiscordId);
            var dm = await discordRecipient.CreateDMChannelAsync();
            await dm.SendMessageAsync($"{question} Y/N");

            while (counter < maxAttempts)
            {
                var response = await _interactive.NextMessageAsync(
                        x => x.Author.Id == recipientDiscordId && x.Channel.Id == dm.Id,
                        timeout: TimeSpan.FromMinutes(2));

                if (!response.IsSuccess)
                {
                    counter++;
                    await dm.SendMessageAsync($"You did not respond in time. Please try again. You have {maxAttempts - counter} more attempts.");
                    continue;
                }

                if (response.Value.Content.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else if (response.Value.Content.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                else
                {
                    counter++;
                    if (counter < maxAttempts)
                    {
                        await dm.SendMessageAsync($"You did not enter a valid response. Please try again. You have {maxAttempts - counter} more attempts.");
                    }
                }
            }

            return null;
        }

        public async Task<bool> UserCanSendMessageAsync(User recipient, User sender, string objectType)
        {
            if (sender.UserId == recipient.UserId ||
                await _helpers.UserIsWhitelistedAsync(recipient.DiscordId, sender.UserId))
            {
                return true;
            }

            if (await _helpers.UserIsBlacklistedAsync(recipient.DiscordId, sender.UserId))
            {
                return false;
            }

            await GetUserConsentAsync(recipient, sender, objectType);
            return false;
        }

        public static async Task AddButtonsAsync(RestDMChannel dm, int mealId, string text)
        {
            var builder = new ComponentBuilder()
                .WithButton("Yes", $"yes_{mealId}")
                .WithButton("No", "no");

            await dm.SendMessageAsync(text, components: builder.Build());
        }

        public async Task SendRecipeFileAsync(string recipe, RestDMChannel dm)
        {
            var name = Path.ChangeExtension(Path.GetRandomFileName(), ".txt");
            var path = Path.Combine(Path.GetTempPath(), name);
            WriteRecipeFile(path, recipe);
            await dm.SendFileAsync(path);
        }

        public static void WriteRecipeFile(string filePath, string recipe)
        {
            using (StreamWriter writer = new(filePath))
            {
                writer.Write(recipe);
            }
        }
    }
}
