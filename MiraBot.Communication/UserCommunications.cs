using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using MiraBot.DataAccess;

namespace MiraBot.Communication
{
    public class UserCommunications : InteractionModuleBase<SocketInteractionContext>
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
            await dm.SendMessageAsync($"{owner.UserName} sent you a recipe for \"{meal.Name}\"! Here's the recipe for it.");
            await SendRecipeFileAsync(meal.Recipe, dm);
            await AddButtonsAsync(dm, meal.MealId, "Do you want to save this recipe?");
        }

        public async Task AddButtonsAsync(RestDMChannel dm, int mealId, string text)
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

        public void WriteRecipeFile(string filePath, string recipe)
        {
            using (StreamWriter writer = new(filePath))
            {
                writer.Write(recipe);
            }
        }
    }
}
