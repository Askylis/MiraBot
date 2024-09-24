using Discord.Interactions;
using Fergun.Interactive;
using Microsoft.IdentityModel.Tokens;
using MiraBot.Common;
using MiraBot.Communication;
using MiraBot.DataAccess;
using MiraBot.GroceryAssistance;
using MiraBot.Permissions;

namespace MiraBot.Modules
{
    [NotBanned]
    public class GroceryAssistantModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GroceryAssistant _groceryAssistant;
        private readonly InteractiveService _interactiveService;
        private readonly ModuleHelpers _helpers;
        private readonly UserCommunications _comms;
        internal static int modifyValue = 0;
        internal const int maxNameLength = 50;
        internal const int discordMsgLimit = 2000;
        internal const int maxIngredients = 100;
        internal const int selectMenuLimit = 24;
        internal const int maxIngredientLength = 1500;
        internal const int maxRecipeLength = 65535;
        public GroceryAssistantModule(GroceryAssistant groceryAssistant, InteractiveService interactiveService, 
            ModuleHelpers moduleHelpers, UserCommunications comms)
        {
            _groceryAssistant = groceryAssistant;
            _interactiveService = interactiveService;
            _helpers = moduleHelpers;
            _comms = comms;
        }

        [SlashCommand("gaadd", "Add a new meal and associated ingredients.")]
        public async Task AddMealAsync()
        {
            if (! await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            string? recipe = null;
            await RespondAsync("What's the name of your new meal?");
            string mealName = await GetValidNameAsync(isIngredient: false);
            if (mealName is null)
            {
                return;
            }
            var ingredients = await AddIngredientsAsync();
            if (await UserWantsToAddRecipe())
            {
                await ReplyAsync("Okay, go ahead and send me the recipe for this meal! Make sure to copy/paste it. Don't send a file or image.");
                recipe = await GetRecipeAsync();
                if (recipe.IsNullOrEmpty())
                {
                    return;
                }
            }
            await _groceryAssistant.AddMealAsync(mealName, ingredients, Context.User.Id, recipe);
            await ReplyAsync($"All done! Added \"{mealName}\" and its {ingredients.Count} ingredients!");
        }


        [SlashCommand("gadelete", "Lets you delete one of your saved meals.")]
        public async Task DeleteMealAsync()
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            int index = 0;
            await RespondAsync("One moment, please!");
            var meals = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);

            if (meals.Count < 1)
            {
                await ReplyAsync("You don't have any meals to delete!");
                return;
            }
            while (index != -1)
            {
                index = await _helpers.GetIndexOfUserChoiceAsync(
                meals,
                "Choose which meal you'd like to delete.",
                "Remove this meal from your saved meals.",
                Context,
                "delete-menu",
                meals => meals.Name
                );
                if (index == -1)
                {
                    break;
                }
                await _groceryAssistant.DeleteMealAsync(meals[index].MealId);
                await ReplyAsync($"Okay, removed {meals[index].Name}! Need to remove anything else?");
                meals.RemoveAt(index);
            }
        }


        [SlashCommand("gaedit", "Lets you edit one of your saved meals.")]
        public async Task EditMealAsync()
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            int index = 0;
            int selection = 0;
            await RespondAsync("One moment, please!");
            var meals = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);

            if (meals.Count < 0)
            {
                await ReplyAsync("Sorry, doesn't look like you have any saved meals! You can add meals by using /gaadd.");
                return;
            }

            index = await _helpers.GetIndexOfUserChoiceAsync(
            meals,
            "Choose which meal you'd like to edit.",
            "Modify this meal",
            Context,
            "edit-menu",
            meal => meal.Name
            );

            if (index == -1)
            {
                return;
            }

            var options = new List<string>
                    {
                        "Edit meal name.",
                        "Edit ingredients.",
                        "Edit both.",
                        "Modify the recipe."
                    };
            await ReplyAsync("Do you want to edit the name, ingredients, both, or modify its recipe?");
            await _helpers.GetIndexOfUserChoiceAsync(
                options,
                "Choose what to edit.",
                null,
                Context,
                "edit-menu",
                option => option
                );
            selection = modifyValue;
            modifyValue = 0;
            Meal selectedMeal = meals[index];
            string mealName = "";
            var ingredients = string.Join(", ", meals[index].Ingredients.Select(i => i.Name));
            List<string> ingNames;
            switch (selection)
            {
                case -1:
                    return;
                case 0:
                    await ReplyAsync($"Sure! What's the new name for {selectedMeal.Name}?");
                    mealName = await GetValidNameAsync(false);
                    if (mealName is null)
                        return;
                    await _groceryAssistant.EditMealAsync(selectedMeal);
                    break;
                case 1:
                    await ReplyAsync($"This meal's current ingredients are: {ingredients}");
                    ingNames = await AddIngredientsAsync();
                    foreach (var ing in ingNames)
                    {
                        selectedMeal.Ingredients.Add(new Ingredient { Name = ing, OwnerId = selectedMeal.OwnerId});
                    }
                    await _groceryAssistant.EditMealAsync(selectedMeal);
                    break;
                case 2:
                    await ReplyAsync($"All right, what's the new name for {selectedMeal.Name}?");
                    mealName = await GetValidNameAsync(false);
                    if (mealName is null)
                        return;
                    await ReplyAsync($"This meal's current ingredients are: {ingredients}");
                    ingNames = await AddIngredientsAsync();
                    foreach (var ing in ingNames)
                    {
                        selectedMeal.Ingredients.Add(new Ingredient { Name = ing, OwnerId = selectedMeal.OwnerId });
                    }
                    await _groceryAssistant.EditMealAsync(selectedMeal);
                    break;
                case 3:
                    if (selectedMeal.Recipe.IsNullOrEmpty())
                    {
                        await AddRecipeAsync(false, selectedMeal.MealId);
                    }
                    else
                    {
                        await SendRecipeFileAsync(selectedMeal.Recipe);
                        await ReplyAsync("Go ahead and modify it however you'd like, and send the edited version back to me when you're done!");
                        await AddRecipeAsync(true, selectedMeal.MealId);
                    }
                    break;
                default:
                    await ReplyAsync("Something went SERIOUSLY wrong. How did you even manage this? It shouldn't be possible. Go yell at Sky or something I guess.");
                    break;
            }
            await ReplyAsync("All done! That meal has been updated!");
        }


        [SlashCommand("galist", "Lists all meals, along with associated ingredients, that are owned by you.")]
        public async Task ListMealsAsync()
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            await RespondAsync("Gimme just a sec!");
            var meals = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);

            if (meals.Count == 0)
            {
                await ReplyAsync("You have no meals saved.");
                return;
            }

            await ReplyAsync("Here's all your meals! \n");
            if (meals.Count > 20)
            {
                await SendListFileAsync(meals);
            }
            else
            {
                await SendLongMessageAsync(meals: meals, sendIngredients: true);
            }
        }


        [SlashCommand("ga", "Generates a new list of grocery ideas.")]
        public async Task GenerateMealsListAsync()
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            var meals = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);
            var mealCount = meals.Count;
            await RespondAsync($"Okay, tell me how many meals you want! You have {mealCount} total meals. You can also select \"0\" to cancel this command.");
            int numberOfMeals = await _helpers.GetValidNumberAsync(0, mealCount, Context);
            int index = 0;

            if (numberOfMeals == 0)
            {
                await ReplyAsync("Okay, no problem! I'll be here if you need me!");
                return;
            }

            var selectedMeals = await _groceryAssistant.GenerateMealIdeasAsync(numberOfMeals, Context.User.Id);
            await FollowupAsync("Here's what we have so far! \n");
            await SendLongMessageAsync(meals: selectedMeals, sendIngredients: true);

            while (numberOfMeals < mealCount && index > -1)
            {
                index = await _helpers.GetIndexOfUserChoiceAsync(
                    selectedMeals,
                    "Choose override preference",
                    "Remove this meal from your selected meals.",
                    Context,
                    "override-menu",
                    meal => meal.Name
                    );
                if (index >= 0)
                {
                    string removedMeal = selectedMeals[index].Name;
                    var newMeals = await _groceryAssistant.ReplaceMealAsync(selectedMeals, index, Context.User.Id);
                    await ReplyAsync($"Removed **{removedMeal}** and added **{newMeals[newMeals.Count - 1].Name}**! Here's your updated meals list!\n");
                    await SendLongMessageAsync(meals: newMeals, sendIngredients: true);
                    selectedMeals = newMeals;
                }
            }
            await ReplyAsync("Here you go!");
            await SendSelectionFileAsync(selectedMeals);
            var dateNow = DateOnly.FromDateTime(DateTime.Now);
            var updates = new List<Task>();
            var files = new List<Task>();
            foreach (var meal in selectedMeals)
            {
                List<string> ingredients = meal.Ingredients.Select(i => i.Name).ToList();
                updates.Add(_groceryAssistant.EditMealAsync(meal));
                if (meal.Recipe is not null)
                {
                    files.Add(SendRecipeFileAsync(meal.Recipe));
                }
            }
            await Task.WhenAll(updates);
        }


        [SlashCommand("gaconvert", "Converts old Grocery Assistant meals files into database entries.")]
        public async Task ConvertMealsFileAsync()
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }
            await RespondAsync("I'll help you convert the old meals file from the original Grocery Assistant to a format that I can understand! Just send your meals file and I'll do the rest! Keep in mind that I will not convert any meals you have that have no ingredients listed.");
            var mealsFile = await _interactiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id && x.Channel.Id == Context.Channel.Id,
            timeout: TimeSpan.FromMinutes(2));
            var message = mealsFile.Value;

            if (message.Attachments.Count == 0)
            {
                await FollowupAsync("I don't see an attachment in your last message. Make sure you're sending me the meals file itself, and not copy pasting its content!");
                return;
            }

            var attachment = message.Attachments.FirstOrDefault();

            if (attachment == null || !attachment.Filename.EndsWith(".txt"))
            {
                await FollowupAsync("Please make sure that you're only uploading a .txt file!");
                return;
            }

            var fileContent = await _groceryAssistant.DownloadFileContentAsync(attachment.Url);

            await ReplyAsync("All right, gimme just a sec!");

            var mealsText = fileContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var invalidNames = await _groceryAssistant.CheckNamesFromConversion(mealsText, Context.User.Id);

            if (!invalidNames.IsNullOrEmpty())
            {
                await ReplyAsync("Sorry, I can't finish this quite yet! You either have some meal and/or ingredient names that are too long, or you have duplicate meal names.");
                await SendLongMessageAsync(invalidNames);
                await ReplyAsync("Please remove or edit any duplicate meals, and shorten invalid meal names to 50 characters or less, and then re-run this command!");
                return;
            }

            var meals = await _groceryAssistant.ConvertMealsFileAsync(mealsText, Context.User.Id);
            if (meals.Count == 0)
            {
                await ReplyAsync("Sorry, it doesn't look like there's any valid meals in here!");
            }
            else
            {
                await ReplyAsync($"All right, all done! Converted and saved all {meals.Count} meals!");
            }
        }


        [SlashCommand("gaaddrecipe", "Add a recipe to an existing meal.")]
        public async Task AddRecipeAsync(bool isEdit, int mealId = 0)
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }

            Meal meal = new();

            if (mealId == 0 && !isEdit)
            {
                var meals = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);
                if (meals.Count == 0)
                {
                    await ReplyAsync("You don't have any meals saved.");
                    return;
                }
                await ReplyAsync("First, select which meal you'd like to add a recipe to.");
                int index = await _helpers.GetIndexOfUserChoiceAsync(
                meals,
                "Choose which meal you'd like to add a recipe to.",
                "Add a recipe to this meal.",
                Context,
                "recipe-menu",
                meal => meal.Name
                );

                if (index == -1)
                {
                    return;
                }
                meal = meals[index];
            }
            else if (isEdit)
            {
                meal = await _groceryAssistant.FindAsync(mealId);
            }
            else
            {
                await ReplyAsync($"Okay, go ahead and send me your recipe for {meal.Name}! You can either copy/paste the recipe to me, or send it in a text file.");
                meal = await _groceryAssistant.FindAsync(mealId);
            }
            meal.Recipe = await GetRecipeAsync();
            if (meal.Recipe.IsNullOrEmpty())
            {
                return;
            }
            await _groceryAssistant.EditMealAsync(meal);
            await ReplyAsync($"Got it! Added that recipe to you meal \"{meal.Name}\"!");
        }

        [SlashCommand("gagetrecipe", "Retrieve a recipe for a specified meal.")]
        public async Task RetrieveRecipeAsync()
        {
            await DeferAsync();
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await FollowupAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }

            Meal meal;
            int mealId = 0;
            var all = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);
            var meals = all.Where(m => !m.Recipe.IsNullOrEmpty()).ToList();
            if (meals.Count == 0)
            {
                await FollowupAsync("You don't have any meals that have a recipe.");
                return;
            }

            await FollowupAsync("Which meal's recipe do you want to see?");
            int index = await _helpers.GetIndexOfUserChoiceAsync(
            meals,
            "Choose which recipe you'd like to view.",
            "Add a recipe to this meal.",
            Context,
            "recipe-menu",
            meal => meal.Name
            );

            if (index == -1)
            {
                return;
            }
            meal = meals[index];
            await SendRecipeFileAsync(meal.Recipe);
        }


        [SlashCommand("gashare", "Share a recipe with another user.")]
        public async Task ShareRecipeAsync(string recipientName)
        {
            if (!await _helpers.UserExistsAsync(Context.User.Id))
            {
                await RespondAsync("It doesn't look like you've registered with me yet. Please use /register so you can start using commands!");
                return;
            }

            var recipient = await _helpers.GetUserByNameAsync(recipientName);
            var owner = await _helpers.GetUserByDiscordIdAsync(Context.User.Id);

            if (recipient is null)
            {
                await RespondAsync($"Could not find a user with the username \"{recipientName}\". Please try again with a valid username.");
                return;
            }

            var meals = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);
            var mealsWithRecipes = meals.Where(m =>  m.Recipe != null).ToList();
            if (mealsWithRecipes.Count == 0)
            {
                await RespondAsync("You have no saved meals that have recipes attached to them.");
                return;
            }
            await RespondAsync("Which meal would you like to share?");
            int index = await _helpers.GetIndexOfUserChoiceAsync(
            mealsWithRecipes,
            "Choose which recipe you'd like to share.",
            "Share this meal.",
            Context,
            "share-menu",
            meal => meal.Name
            );

            if (index == -1)
            {
                return;
            }
            var share = mealsWithRecipes[index];
            await _comms.SendRecipeAsync(recipient, owner, mealsWithRecipes[index]);
            await ReplyAsync($"Okay, sent that recipe to {recipient.UserName}!");
        }

        public async Task<bool> UserWantsToAddRecipe()
        {
            bool isValid = false;
            var wantsRecipe = false;
            await ReplyAsync("Do you want to add a recipe to this meal? Y/N");

            while (!isValid)
            {
                var response = await _interactiveService.NextMessageAsync(
                        x => x.Author.Id == Context.User.Id && x.Channel.Id == Context.Channel.Id,
                        timeout: TimeSpan.FromMinutes(2));

                if (!response.IsSuccess)
                {
                    await ReplyAsync("You did not respond in time.");
                    break;
                }

                if (response.Value.Content.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    wantsRecipe = true;
                    isValid = true;
                }
                else if (response.Value.Content.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    isValid = true;
                }
                else
                {
                    await ReplyAsync("You did not enter a valid response.");
                }
            }

            return wantsRecipe;
        }


        public async Task SendLongMessageAsync(List<string>? input = null, List<Meal>? meals = null, bool sendIngredients = false)
        {
            var sentMessage = _groceryAssistant.SendLongMessage(input, meals, sendIngredients);

            foreach (var message in sentMessage)
            {
                await ReplyAsync(message);
            }
        }

        public async Task<string> GetRecipeAsync()
        {
            var isValid = false;
            string recipe = string.Empty;

            while (!isValid)
            {
                var response = await _interactiveService.NextMessageAsync(
                        x => x.Author.Id == Context.User.Id && x.Channel.Id == Context.Channel.Id,
                        timeout: TimeSpan.FromMinutes(2));

                if (!response.IsSuccess)
                {
                    await ReplyAsync("You did not respond in time.");
                    return string.Empty;
                }

                if (response.Value.Attachments.Count == 1)
                {
                    var attachment = response.Value.Attachments.FirstOrDefault();
                    if (attachment == null || !attachment.Filename.EndsWith(".txt"))
                    {
                        await FollowupAsync("It looks like you tried to send a recipe file, but it's not in the correct format. Make sure that it's a .txt file.");
                        continue;
                    }

                    return await _groceryAssistant.DownloadFileContentAsync(attachment.Url);
                }

                if (response.Value.Content.Length > maxRecipeLength)
                {
                    await ReplyAsync($"Your recipe is too long! It is {response.Value.Content.Length} characters long, but can be no longer than {maxRecipeLength} characters long.");
                    continue;
                }

                isValid = true;
                recipe = response.Value.Content;
            }

            return recipe;
        }


        public async Task SendListFileAsync(List<Meal> mealsList)
        {
            var name = Path.ChangeExtension(Path.GetRandomFileName(), ".txt");
            var path = Path.Combine(Path.GetTempPath(), name);
            _groceryAssistant.WriteListFile(path, mealsList);
            await FollowupWithFileAsync(path);
        }

        public async Task SendSelectionFileAsync(List<Meal> selectedMeals)
        {
            var name = Path.ChangeExtension(Path.GetRandomFileName(), ".txt");
            var path = Path.Combine(Path.GetTempPath(), name);
            _groceryAssistant.WriteSelectionFile(path, selectedMeals);
            await FollowupWithFileAsync(path);
        }

        public async Task SendRecipeFileAsync(string recipe)
        {
            var name = Path.ChangeExtension(Path.GetRandomFileName(), ".txt");
            var path = Path.Combine(Path.GetTempPath(), name);
            _groceryAssistant.WriteRecipeFile(path, recipe);
            await FollowupWithFileAsync(path);
        }

        public async Task<string> GetValidNameAsync(bool isIngredient, string? name = null)
        {
            bool isValid = false;

            while (!isValid)
            {
                if (string.IsNullOrEmpty(name))
                {
                    var response = await _interactiveService.NextMessageAsync(
                        x => x.Author.Id == Context.User.Id && x.Channel.Id == Context.Channel.Id,
                        timeout: TimeSpan.FromMinutes(2)
                    );

                    if (!response.IsSuccess)
                    {
                        await ReplyAsync("You did not respond in time.");
                        break;
                    }

                    name = response.Value.Content;
                }

                int maxLength = isIngredient ? maxIngredientLength : maxNameLength;

                if (name.Length > maxLength)
                {
                    await ReplyAsync($"Your input contains too many characters! {(isIngredient ? "Ingredients" : "Meal names")} can't contain more than {maxLength} characters. Shorten your input and try again!");
                    name = null;
                    continue;
                }

                if (await _groceryAssistant.IsDuplicateNameAsync(name, Context.User.Id) && !isIngredient)
                {
                    await ReplyAsync($"I'm sorry, I can't add {name} as a meal because this meal already exists in your database. Do you want to name it something else?");
                    name = null;
                    continue;
                }

                isValid = true;
            }

            return name;
        }


        public async Task<List<string>> GetIngredientsAsync(string response)
        {
            string[] ingredients = response.Split(',');
            var trimmedIngredients = ingredients.Select(i => i.Trim()).ToList();
            trimmedIngredients = trimmedIngredients.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (trimmedIngredients.Count > maxIngredients)
            {
                await ReplyAsync($"This meal has too many ingredients! You have {trimmedIngredients.Count} ingredients, but max is {maxIngredients}. Correct this, and do /addmeal again once you're ready to try again.");
                return null;
            }
            return trimmedIngredients;
        }

        public async Task<List<string>> AddIngredientsAsync()
        {
            await ReplyAsync("Okay, now what are the ingredients for this meal? Separate each ingredient with a comma!");
            var response = await GetValidNameAsync(isIngredient: true);
            var trimmedIngredients = await GetIngredientsAsync(response);
            for (int i = 0; i < trimmedIngredients.Count; i++)
            {
                trimmedIngredients[i] = await GetValidNameAsync(isIngredient: true, trimmedIngredients[i]);
            }

            return trimmedIngredients;
        }
    }
}