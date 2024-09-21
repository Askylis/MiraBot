﻿using Discord.Interactions;
using Fergun.Interactive;
using Microsoft.IdentityModel.Tokens;
using MiraBot.DataAccess;
using MiraBot.GroceryAssistance;

namespace MiraBot.Modules
{
    public class GroceryAssistantModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GroceryAssistant _groceryAssistant;
        private readonly InteractiveService _interactiveService;
        private readonly GroceryAssistantComponents _components;
        private readonly ModuleHelpers _helpers;
        internal static int modifyValue = 0;
        internal const int maxNameLength = 50;
        internal const int discordMsgLimit = 2000;
        internal const int maxIngredients = 100;
        internal const int selectMenuLimit = 24;
        internal const int maxIngredientLength = 1500;
        public GroceryAssistantModule(GroceryAssistant groceryAssistant, InteractiveService interactiveService, 
            GroceryAssistantComponents components, ModuleHelpers moduleHelpers)
        {
            _groceryAssistant = groceryAssistant;
            _interactiveService = interactiveService;
            _components = components;
            _helpers = moduleHelpers;
        }


        [SlashCommand("addmeal", "Add a new meal and associated ingredients.")]
        public async Task AddMealAsync()
        {
            await _groceryAssistant.CheckForNewUserAsync(Context.User.Username, Context.User.Id);
            await RespondAsync("What's the name of your new meal?");
            string mealName = await GetValidNameAsync(isIngredient: false);
            if (mealName is null)
            {
                return;
            }
            var ingredients = await AddIngredientsAsync();
            await _groceryAssistant.AddMealAsync(mealName, ingredients, Context.User.Id);
            await ReplyAsync($"All done! Added \"{mealName}\" and its {ingredients.Count} ingredients!");
        }


        [SlashCommand("deletemeal", "Lets you delete one of your saved meals.")]
        public async Task DeleteMealAsync()
        {
            await _groceryAssistant.CheckForNewUserAsync(Context.User.Username, Context.User.Id);
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
                index = await GetMealIndexAsync(meals,
                "Choose which meal you'd like to delete.",
                "delete-menu",
                "Remove this meal from your saved meals.",
          Context);
                if (index == -1)
                {
                    break;
                }
                await _groceryAssistant.DeleteMealAsync(meals[index].MealId, Context.User.Id);
                await ReplyAsync($"Okay, removed {meals[index].Name}! Need to remove anything else?");
                meals.RemoveAt(index);
            }
        }


        [SlashCommand("editmeal", "Lets you edit one of your saved meals.")]
        public async Task EditMealAsync()
        {
            await _groceryAssistant.CheckForNewUserAsync(Context.User.Username, Context.User.Id);
            int index = 0;
            int selection = 0;
            await RespondAsync("One moment, please!");
            var meals = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);

            if (meals.Count < 0)
            {
                await ReplyAsync("Sorry, doesn't look like you have any saved meals! Add some meals and get back to me! :)");
                return;
            }

            index = await GetMealIndexAsync(meals,
            "Choose which meal you'd like to edit.",
            "edit-menu",
            "Modify this meal",
      Context);

            if (index == -1)
            {
                return;
            }

            var options = new List<string>
                    {
                        "Edit meal name.",
                        "Edit ingredients.",
                        "Edit both."
                    };
            await ReplyAsync("Do you want to edit the name, ingredients, or both?");
            await _components.GenerateMenuAsync(options,
                "Choose what to edit.",
                "edit-menu",
                null,
                Context
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
                    ingNames = selectedMeal.Ingredients.Select(i => i.Name).ToList();
                    await _groceryAssistant.DeleteMealAsync(selectedMeal.MealId, Context.User.Id);
                    await _groceryAssistant.AddMealAsync(mealName, ingNames, Context.User.Id, selectedMeal.Date);
                    break;
                case 1:
                    await ReplyAsync($"This meal's current ingredients are: {ingredients}");
                    ingNames = await AddIngredientsAsync();
                    await _groceryAssistant.DeleteMealAsync(selectedMeal.MealId, Context.User.Id);
                    await _groceryAssistant.AddMealAsync(selectedMeal.Name, ingNames, Context.User.Id, selectedMeal.Date);
                    break;
                case 2:
                    await ReplyAsync($"All right, what's the new name for {selectedMeal.Name}?");
                    mealName = await GetValidNameAsync(false);
                    if (mealName is null)
                        return;
                    await ReplyAsync($"This meal's current ingredients are: {ingredients}");
                    ingNames = await AddIngredientsAsync();
                    await _groceryAssistant.DeleteMealAsync(selectedMeal.MealId, Context.User.Id);
                    await _groceryAssistant.AddMealAsync(mealName, ingNames, Context.User.Id, selectedMeal.Date);
                    break;
                default:
                    await ReplyAsync("Something went SERIOUSLY wrong. How did you even manage this? It shouldn't be possible. Go yell at Sky or something I guess.");
                    break;
            }
            await ReplyAsync("All done! That meal has been updated!");
        }


        [SlashCommand("listmeals", "Lists all meals, along with associated ingredients, that are owned by you.")]
        public async Task ListMealsAsync()
        {
            await _groceryAssistant.CheckForNewUserAsync(Context.User.Username, Context.User.Id);
            await RespondAsync("Gimme just a sec!");
            var meals = await _groceryAssistant.GetAllMealsAsync(Context.User.Id);

            if (!meals.Any())
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
            await _groceryAssistant.CheckForNewUserAsync(Context.User.Username, Context.User.Id);
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
                index = await GetMealIndexAsync(selectedMeals,
                    "Choose override preference",
                    "override-menu",
                    "Remove this meal from your selected meals.",
                    Context);
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
            foreach (var meal in selectedMeals)
            {
                List<string> ingredients = meal.Ingredients.Select(i => i.Name).ToList();
                updates.Add(_groceryAssistant.DeleteMealAsync(meal.MealId, Context.User.Id));
                updates.Add(_groceryAssistant.AddMealAsync(meal.Name, ingredients, Context.User.Id, dateNow));
            }
            await Task.WhenAll(updates);
        }


        [SlashCommand("convert", "Converts old Grocery Assist meals files into a format that Mira can understand.")]
        public async Task ConvertMealsFileAsync()
        {
            await _groceryAssistant.CheckForNewUserAsync(Context.User.Username, Context.User.Id);
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


        public async Task SendLongMessageAsync(List<string>? input = null, List<Meal>? meals = null, bool sendIngredients = false)
        {
            var sentMessage = _groceryAssistant.SendLongMessage(input, meals, sendIngredients);

            foreach (var message in sentMessage)
            {
                await ReplyAsync(message);
            }
        }


        public async Task<int> GetMealIndexAsync(List<Meal> meals, string placeholder, string customId, string? description, SocketInteractionContext ctx)
        {
            int selection;
            if (meals.Count <= selectMenuLimit)
            {
                var names = meals.Select(m => m.Name).ToList();

                await _components.GenerateMenuAsync(names,
                    placeholder,
                    customId,
                    description,
                    ctx);
                selection = modifyValue;
                modifyValue = 0;
            }

            else
            {
                await SendLongMessageAsync(meals: meals);
                selection = await _helpers.GetValidNumberAsync(0, meals.Count, Context);
                selection--;
            }

            return selection;
        }


        public async Task SendListFileAsync(List<Meal> mealsList)
        {
            var fileName = $"{Context.User.Username}ListResults.txt";
            var filePath = _groceryAssistant.GetOutputPath(fileName);
            _groceryAssistant.WriteListFile(filePath, mealsList);
            await FollowupWithFileAsync(filePath);
        }

        public async Task SendSelectionFileAsync(List<Meal> selectedMeals)
        {
            var fileName = $"{Context.User.Username}OutputResults.txt";
            var filePath = _groceryAssistant.GetOutputPath(fileName);
            _groceryAssistant.WriteSelectionFile(filePath, selectedMeals);
            await FollowupWithFileAsync(filePath);
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