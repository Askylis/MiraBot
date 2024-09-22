using Discord.Interactions;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Modules
{
    public class ButtonHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IGroceryAssistantRepository _gaRepository;
        private readonly UsersRepository _usersRepository;

        public ButtonHandler(IGroceryAssistantRepository repository, UsersRepository usersRepository)
        {
            _gaRepository = repository;
            _usersRepository = usersRepository;
        }

        [ComponentInteraction("yes_*")]
        public async Task HandleRecipeSaveYesButton(string mealIdStr)
        {
            if (int.TryParse(mealIdStr, out int mealId))
            {
                var recipient = await _usersRepository.GetUserByDiscordIdAsync(Context.User.Id);
                var meal = await _gaRepository.FindAsync(mealId);
                List<string> ingredients = new();
                foreach (var ingredient in meal.Ingredients)
                {
                    ingredients.Add(ingredient.Name);
                }

                if (recipient != null && meal != null)
                {
                    await _gaRepository.AddMealAsync(meal.Name, ingredients, Context.User.Id, null);

                    await RespondAsync("Added this recipe to your saved meals!");
                }
            }
        }

        [ComponentInteraction("no")]
        public async Task HandleRecipeSaveNoButton()
        {
            await RespondAsync("No worries! I won't save this recipe for you then.");
        }
    }
}
