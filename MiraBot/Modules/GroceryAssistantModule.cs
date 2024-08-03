using Discord.Interactions;
using Fergun.Interactive;
using Microsoft.IdentityModel.Tokens;
using MiraBot.DataAccess;
using MiraBot.GroceryAssistance;

namespace MiraBot.Modules
{
    public class GroceryAssistantModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GroceryAssistant groceryAssistant;
        private readonly InteractiveService interactiveService;
        private readonly GroceryAssistantComponents components;
        internal static int overrideValue = 0;
        internal static int modifyValue = 0;
        internal const int maxNameLength = 50;
        internal const int discordMsgLimit = 2000;
        internal const int maxIngredients = 100;
        internal const int selectMenuLimit = 24;
        internal const int maxIngredientLength = 1500;
        public GroceryAssistantModule(GroceryAssistant groceryAssistant, InteractiveService interactiveService, GroceryAssistantComponents components)
        {
            this.groceryAssistant = groceryAssistant;
            this.interactiveService = interactiveService;
            this.components = components;
        }


        [SlashCommand("addmeal", "Add a new meal and associated ingredients.")]
        public async Task AddMealAsync()
        {
            await RespondAsync("What's the name of your new meal?");
            string mealName = await CheckInvalidNameAsync(isIngredient: false);
            var ingredients = await AddIngredientsAsync();
            await groceryAssistant.AddMealAsync(mealName, ingredients, Context.User.Username);
            await ReplyAsync($"All done! Added \"{mealName}\" and its {ingredients.Count()} ingredients!");
        }


        [SlashCommand("deletemeal", "Lets you delete one of your saved meals.")]
        public async Task DeleteMealAsync()
        {
            int modifySelection = 0;
            await RespondAsync("One moment, please!");
            var meals = await groceryAssistant.GetAllMealsAsync(Context.User.Username);

            if (meals.Count() < 1)
            {
                await ReplyAsync("You don't have any meals to delete!");
                return;
            }

            var mealNames = new List<string>();
            int newNumber = -1;

            if (meals.Count() < selectMenuLimit)
            {
                await ReplyAsync("If you want to delete any of your saved meals, select it now. Otherwise, select \"Nevermind\".");
                foreach (var meal in meals)
                {
                    mealNames.Add(meal.Name);
                }

                while (modifySelection > -1)
                {
                    await components.GenerateMenuAsync(mealNames,
                        "Choose which meal you'd like to delete.",
                        "delete-menu",
                        "Remove this meal from your saved meals.",
                        Context);
                    modifySelection = modifyValue;
                    modifyValue = 0;
                    if (modifySelection >= 0)
                    {
                        await ReplyAsync($"All right, I deleted \"{mealNames[modifySelection]}\" for you! Do you need to delete anything else?");
                        await groceryAssistant.DeleteMealAsync(mealNames[modifySelection], Context.User.Username);
                        mealNames.RemoveAt(modifySelection);
                    }
                }
            }
            else
            {
                await ReplyAsync("Type out the number associated with the meal you'd like to delete! Type \"0\" if you don't want to delete anything.");
                await SendLongMessageAsync(meals: meals);

                while (newNumber < 0)
                {
                    newNumber = await GetValidNumberAsync(0, meals.Count());
                }
                if (newNumber > 0)
                {
                    await ReplyAsync($"All done! Deleted {meals[newNumber - 1].Name}!");
                    await groceryAssistant.DeleteMealAsync(meals[newNumber - 1].Name, Context.User.Username);
                }
                if (newNumber == 0)
                {
                    await ReplyAsync("Okay, I'll be here if you need me for anything else!");
                }

            }
        }


        [SlashCommand("editmeal", "Lets you edit one of your saved meals.")]
        public async Task EditMealAsync()
        {
            int modifySelection = 1;
            int selectionValue = 1;
            await RespondAsync("One moment, please!");
            var meals = await groceryAssistant.GetAllMealsAsync(Context.User.Username);
            var mealsCount = meals.Count();
            if (mealsCount == 0)
            {
                await ReplyAsync("Sorry, doesn't look like you have any saved meals! Add some meals and get back to me! :)");
                return;
            }
            var mealNames = meals.Select(meal => meal.Name).ToList();
            Meal selectedMeal = new();
            string selectedMealName = "";

            if (mealsCount <= selectMenuLimit)
            {
                await components.GenerateMenuAsync(
                mealNames,
                "Choose which meal you'd like to edit.",
                "edit-menu",
                "Edit this meal.",
                Context);
                //I KNOW THAT USING STATIC FIELDS LIKE THIS IS WRONG AND UGLY AND BAD
                //BUT I HAVE NO IDEA HOW ELSE TO GET THE VALUES I NEED FROM THE MENU HANDLERS
                //DON'T YELL AT ME :(
                //I was gonna ask for help with finding a better solution
                modifySelection = modifyValue;
                modifyValue = 0;
            }

            else
            {
                await SendLongMessageAsync(meals: meals, sendIngredients: false);
                await ReplyAsync("Type in the number associated with the meal you'd like to update. Otherwise, type \"0\".");
                int selection = await GetValidNumberAsync(0, meals.Count());
                if (selection == 0)
                {
                    await ReplyAsync("Sure, no problem!");
                    return;
                }
                modifySelection = selection -1;
            }

            if (modifySelection >= 0)
            {
                selectedMealName = meals[modifySelection].Name;
                await ReplyAsync($"You selected {selectedMealName}!");
                selectedMeal = meals[modifySelection];
                var menuOptions = new List<string>
                    {
                        "Edit meal name.",
                        "Edit ingredients.",
                        "Edit both."
                    };

                await components.GenerateMenuAsync(
                    menuOptions,
                    "Choose what you'd like to edit.",
                    "edit-menu",
                    optionDescription: null,
                    Context);

                selectionValue = modifyValue;
                modifyValue = 0;
            }
            else
            {
                await ReplyAsync("Okay, no problem!");
                return;
            }

            string mealName = "";
            List<string> ingNames = new();
            var ingredients = string.Join(", ", meals[modifySelection].Ingredients.Select(i => i.Name));
            switch (selectionValue)
            {
                case 0:
                    await ReplyAsync("Sure! What's the new name for this meal?");
                    mealName = await CheckInvalidNameAsync(false);
                    ingNames = selectedMeal.Ingredients.Select(i => i.Name).ToList();
                    await groceryAssistant.DeleteMealAsync(selectedMealName, Context.User.Username);
                    await groceryAssistant.AddMealAsync(mealName, ingNames, Context.User.Username, selectedMeal.Date);
                    break;
                case 1:
                    await ReplyAsync($"This meal's current ingredients are: {ingredients}");
                    ingNames = await AddIngredientsAsync();
                    await groceryAssistant.DeleteMealAsync(selectedMealName, Context.User.Username);
                    await groceryAssistant.AddMealAsync(selectedMealName, ingNames, Context.User.Username, selectedMeal.Date);
                    break;
                case 2:
                    await ReplyAsync("All right, what's the new name for this meal?");
                    mealName = await CheckInvalidNameAsync(false);
                    await ReplyAsync($"This meal's current ingredients are: {ingredients}");
                    ingNames = await AddIngredientsAsync();
                    await groceryAssistant.DeleteMealAsync(selectedMealName, Context.User.Username);
                    await groceryAssistant.AddMealAsync(mealName, ingNames, Context.User.Username, selectedMeal.Date);
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
            await RespondAsync("Gimme just a sec!");
            var meals = await groceryAssistant.GetAllMealsAsync(Context.User.Username);

            if (!meals.Any())
            {
                await ReplyAsync("You have no meals saved.");
                return;
            }

            await ReplyAsync("Here's all your meals! \n");
            //fix this if statement to instead check for message length, rather than number of meals
            if (meals.Count() > 20) 
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
            var allMeals = await groceryAssistant.GetAllMealsAsync(Context.User.Username);
            var mealCount = allMeals.Count();
            await RespondAsync($"Okay, tell me how many meals you want! You have {mealCount} total meals. You can also select \"0\" to cancel this command.");
            int numberOfMeals = await GetValidNumberAsync(0, mealCount);
            int overrideSelection = 0;
            int overrideChoice = -1;

            if (numberOfMeals == 0)
            {
                await ReplyAsync("Okay, no problem! I'll be here if you need me!");
                return;
            }

            var selectedMeals = await groceryAssistant.GenerateMealIdeasAsync(numberOfMeals, Context.User.Username);

            await FollowupAsync("Here's what we have so far! \n");
            await SendLongMessageAsync(meals: selectedMeals, sendIngredients: true);

            while (overrideSelection > -1 && numberOfMeals < mealCount && numberOfMeals <= (selectMenuLimit))
            {
                await ReplyAsync("If you want to replace any of these meals, select it now. Otherwise, select \"No override\".");
                await components.GenerateMenuAsync(selectedMeals,
                    "Choose override preference",
                    "override-menu",
                    "Remove this meal from your selected meals.",
                    Context,
                    "No override",
                    "Proceed with your currently selected meals.");

                overrideSelection = overrideValue;
                overrideValue = 0;
                if (overrideSelection >= 0)
                {
                    string removedMeal = selectedMeals[overrideSelection].Name;
                    var newMeals = await groceryAssistant.ReplaceMealAsync(selectedMeals, overrideSelection, Context.User.Username);
                    await ReplyAsync($"Removed **{removedMeal}** and added **{newMeals.Last().Name}**! Here's your updated meals list!\n");
                    await SendLongMessageAsync(meals: newMeals, sendIngredients: true);
                    selectedMeals = newMeals;
                }
            }

            while (numberOfMeals < mealCount && numberOfMeals >= selectMenuLimit)
            {
                await ReplyAsync("If you want to replace any of these meals, type the number associated with it. Otherwise, type \"0\".");
                overrideChoice = await GetValidNumberAsync(0, selectedMeals.Count());

                if (overrideChoice == 0)
                {
                    await ReplyAsync("Okay, no problem! I'll be here if you need me!");
                    return;
                }

                string removedMeal = selectedMeals[overrideChoice - 1].Name;
                var newMeals = await groceryAssistant.ReplaceMealAsync(selectedMeals, overrideChoice - 1, Context.User.Username);
                await ReplyAsync($"Removed **{removedMeal}** and added **{newMeals.Last().Name}**! Here's your updated meals list!\n");
                await SendLongMessageAsync(meals: newMeals);
                selectedMeals = newMeals;
            }
            await ReplyAsync("Here you go!");
            await SendSelectionFileAsync(selectedMeals);
            var dateNow = DateOnly.FromDateTime(DateTime.Now);
            //I was doing lots of reading on async code and tasks, trying to understand the ins and outs of it properly
            //so I can use it as efficiently as possible. Microsoft's documentation for async code said that if you have a bunch of tasks that
            //need to be ran at the same time, to handle them this way instead of awaiting each one sequentially. idk if I did this right,
            //but you can yell at me if I did a dumb :)
            var updates = new List<Task>();
            List<string> ingredients = new();
            foreach (var meal in selectedMeals)
            {
                ingredients = meal.Ingredients.Select(i => i.Name).ToList();
                updates.Add(groceryAssistant.DeleteMealAsync(meal.Name, Context.User.Username));
                updates.Add(groceryAssistant.AddMealAsync(meal.Name, ingredients, Context.User.Username, dateNow));
                ingredients.Clear();
            }
            await Task.WhenAll(updates);
        }


        [SlashCommand("convert", "Converts old Grocery Assist meals files into a format that Mira can understand.")]
        public async Task ConvertMealsFileAsync()
        {
            await RespondAsync("I'll help you convert the old meals file from the original Grocery Assistant to a format that I can understand! Just send your meals file and I'll do the rest! Keep in mind that I will not convert any meals you have that have no ingredients listed.");
            var mealsFile = await interactiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id && x.Channel.Id == Context.Channel.Id,
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

            var fileContent = await groceryAssistant.DownloadFileContentAsync(attachment.Url);

            await ReplyAsync("All right, gimme just a sec!");

            var mealsText = fileContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var invalidNames = await groceryAssistant.CheckNamesFromConversion(mealsText, Context.User.Username);

            if (!invalidNames.IsNullOrEmpty())
            {
                await ReplyAsync("Sorry, I can't finish this quite yet! You either have some meal and/or ingredient names that are too long, or you have duplicate meal names.");
                await SendLongMessageAsync(invalidNames);
                await ReplyAsync("Please remove or edit any duplicate meals, and shorten invalid meal names to 50 characters or less, and then re-run this command!");
                return;
            }

            var meals = await groceryAssistant.ConvertMealsFileAsync(mealsText, Context.User.Username);
            if (meals.Count() == 0)
            {
                await ReplyAsync("Sorry, it doesn't look like there's any valid meals in here!");
            }
            else
            {
                await ReplyAsync($"All right, all done! Converted and saved all {meals.Count()} meals!");
            }
        }


        public async Task SendLongMessageAsync(List<string> input = null, List<Meal> meals = null, bool sendIngredients = false)
        {
            var sentMessage = groceryAssistant.SendLongMessage(input, meals, sendIngredients);

            foreach (var message in sentMessage)
            {
                await ReplyAsync(message);
            }
        }


        public async Task SendListFileAsync(List<Meal> mealsList)
        {
            var fileName = $"{Context.User.Username}ListResults.txt";
            var filePath = groceryAssistant.GetOutputPath(fileName);
            groceryAssistant.WriteListFile(filePath, mealsList);
            await FollowupWithFileAsync(filePath);
        }

        public async Task SendSelectionFileAsync(List<Meal> selectedMeals)
        {
            var fileName = $"{Context.User.Username}OutputResults.txt";
            var filePath = groceryAssistant.GetOutputPath(fileName);
            groceryAssistant.WriteSelectionFile(filePath, selectedMeals);
            await FollowupWithFileAsync(filePath);
        }


        public async Task<int> GetValidNumberAsync(int minNumber, int maxNumber)
        {
            int userChoice = 0;
            bool isValid = false;

            while (!isValid)
            {
                var input = await interactiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id && x.Channel.Id == Context.Channel.Id,
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


        public async Task<string> CheckInvalidNameAsync(bool isIngredient, string? name = null, int maxLength = maxNameLength)
        {
            bool isValid = false;

            while (!isValid)
            {
                if (string.IsNullOrEmpty(name))
                {
                    var response = await interactiveService.NextMessageAsync(
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

                if (groceryAssistant.CheckForInvalidName(name, maxLength))
                {
                    isValid = true;
                }
                else
                {
                    if (maxLength <= 50)
                    {
                        await ReplyAsync($"{name} isn't a valid name. Make sure meal/ingredient names aren't longer than {maxLength}. Shorten it and try again, please!");
                    }
                    if (maxLength > 50)
                    {
                        await ReplyAsync($"This input isn't valid! It can't be longer than {maxLength} characters. Shorten it and then try again, please!");
                    }
                    name = null;
                }

                if (name is not null && await groceryAssistant.HasDuplicateNameAsync(name, Context.User.Username) && !isIngredient)
                {
                    isValid = false;
                    await ReplyAsync($"I'm sorry, I can't add {name} as a meal because this meal already exists in your database. Do you want to name it something else?");
                    name = null;
                }
            }

            return name;
        }

        public async Task<List<string>> GetIngredientsAsync(string response)
        {
            string[] ingredients = response.Split(',');
            var trimmedIngredients = ingredients.Select(i => i.Trim()).ToList();
            trimmedIngredients = trimmedIngredients.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (trimmedIngredients.Count() > maxIngredients)
            {
                await ReplyAsync($"This meal has too many ingredients! You have {trimmedIngredients.Count()} ingredients, but max is {maxIngredients}. Correct this, and do /addmeal again once you're ready to try again.");
                return null;
            }
            return trimmedIngredients;
        }

        public async Task<List<string>> AddIngredientsAsync()
        {
            await ReplyAsync("Okay, now what are the ingredients for this meal? Separate each ingredient with a comma!");
            var response = await CheckInvalidNameAsync(isIngredient: true, maxLength: maxIngredientLength);
            var trimmedIngredients = await GetIngredientsAsync(response);
            for (int i = 0; i < trimmedIngredients.Count(); i++)
            {
                trimmedIngredients[i] = await CheckInvalidNameAsync(isIngredient: true, trimmedIngredients[i]);
            }

            return trimmedIngredients;
        }
    }
}