using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MiraBot.DataAccess;

namespace MiraBot.Communication
{
    public class UserCommunications
    {
        private readonly DiscordSocketClient _client;
        public UserCommunications(DiscordSocketClient client)
        {
            _client = client;
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

            await AddButtonsAsync(dm, meal.MealId, $"{owner.UserName} sent you a recipe for \"{meal.Name}\"! Would you like to save this recipe for yourself?");
        }

        public async Task AddButtonsAsync(RestDMChannel dm, int mealId, string text)
        {
            var builder = new ComponentBuilder()
                .WithButton("Yes", $"yes_{mealId}")
                .WithButton("No", "no");

            await dm.SendMessageAsync(text, components: builder.Build());
        }
    }
}
