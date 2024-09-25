using Discord.Interactions;
using Discord.WebSocket;
using MiraBot.DataAccess.Repositories;
using MiraBot.Communication;
using MiraBot.GroceryAssistance;

namespace MiraBot.Modules
{
    public class ButtonHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IGroceryAssistantRepository _gaRepository;
        private readonly IUsersRepository _usersRepository;
        private readonly GroceryAssistant _ga;

        public ButtonHandler(IGroceryAssistantRepository repository, IUsersRepository usersRepository, GroceryAssistant ga)
        {
            _gaRepository = repository;
            _usersRepository = usersRepository;
            _ga = ga;
        }

        [ComponentInteraction("yes_*")]
        public async Task HandleRecipeSaveYesButton(string mealIdStr)
        {
            if (int.TryParse(mealIdStr, out int mealId))
            {
                var recipient = await _usersRepository.GetUserByDiscordIdAsync(Context.User.Id);
                var meal = await _gaRepository.FindAsync(mealId);
                List<string> ingredients = [];
                foreach (var ingredient in meal.Ingredients)
                {
                    ingredients.Add(ingredient.Name);
                }

                if (recipient != null && meal != null)
                {
                    if (await _gaRepository.IsDuplicateNameAsync(meal.Name, recipient.DiscordId))
                    {
                        string newName;
                        int counter = 1;
                        await RespondAsync($"The name of this shared meal is {meal.Name}, but you already have a meal with that name.");
                        await RespondAsync("I'm going to set the name of this meal to a default name, and you can edit it later.");
                        do
                        {
                            newName = $"{meal.Name}_{counter}";
                            counter++;
                        }
                        while (await _gaRepository.IsDuplicateNameAsync(newName, recipient.DiscordId));
                        meal.Name = newName;
                    }
                    await _gaRepository.AddMealAsync(meal.Name, ingredients, Context.User.Id, meal.Recipe, null);
                    await RespondAsync($"Added \"{meal.Name}\" to your saved meals!");

                    if (Context.Interaction is SocketMessageComponent component)
                    {
                        await component.Message.DeleteAsync();
                    }
                }
            }
        }

        [ComponentInteraction("no")]
        public async Task HandleRecipeSaveNoButton()
        {
            await RespondAsync("No worries! I won't save this recipe for you then.");
            if (Context.Interaction is SocketMessageComponent component)
            {
                await component.Message.DeleteAsync();
            }
        }
    }
}
