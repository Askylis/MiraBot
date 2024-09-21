using Microsoft.Extensions.Logging;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System.Text;
using System.Text.RegularExpressions;

namespace MiraBot.GroceryAssistance
{
    public class GroceryAssistant
    {
        private readonly IGroceryAssistantRepository _groceryAssistantRepository;
        private readonly UsersRepository _usersRepository;

        private readonly ILogger<GroceryAssistant> _logger;

        public GroceryAssistant(
            IGroceryAssistantRepository groceryAssistantRepository,
            ILogger<GroceryAssistant> logger,
            UsersRepository usersRepository)
        {
            _groceryAssistantRepository = groceryAssistantRepository;
            _logger = logger;
            _usersRepository = usersRepository;
        }

        public async Task AddMealAsync(string mealName, List<string> ingredients, ulong discordId, DateOnly? date = null)
        {
            await _groceryAssistantRepository.AddMealAsync(mealName, ingredients, discordId, date);
        }

        public async Task DeleteMealAsync(int mealId, ulong discordId)
        {
            await _groceryAssistantRepository.DeleteMealAsync(mealId, discordId);
        }

        public async Task<List<Meal>> GetAllMealsAsync(ulong discordId)
        {
            return await _groceryAssistantRepository.GetAllMealsAsync(discordId);
        }

        public async Task CheckForNewUserAsync(string userName, ulong discordId)
        {
            if (!await _usersRepository.UserExistsAsync(discordId))
            {
                _logger.LogTrace("This user does not exist! Adding user now.");
                var user = new User
                {
                    DiscordId = discordId,
                    UserName = userName
                };
                await _usersRepository.AddNewUserAsync(user);
            }
        }

        public IEnumerable<string> TrimIngredients(string ingredientsInput)
        {
            string[] ingredients = ingredientsInput.Split(",");
            var trimmedIngredients = ingredients.Select(i => i.Trim());
            return trimmedIngredients;
        }

        public async Task<List<Meal>> GenerateMealIdeasAsync(int numberOfMeals, ulong discordId)
        {
            Random random = new();
            var allMeals = await _groceryAssistantRepository.GetAllMealsAsync(discordId);
            var randomizedMeals = allMeals.OrderBy(m => random.Next()).Take(numberOfMeals);
            return randomizedMeals.ToList();
        }

        public async Task<bool> IsValidNumberAsync(int number, ulong discordId, bool allowZero)
        {
            if ((!allowZero && number <= 0) || (allowZero && number < 0))
            {
                return false;
            }
            return number <= await _groceryAssistantRepository.CountMealsByUserAsync(discordId);
        }

        public async Task<List<Meal>> ConvertMealsFileAsync(string[] mealsFile, ulong discordId)
        {
            var meals = new List<Meal>();
            var currentMeal = new Meal();
            Ingredient currentIngredient;
            var invalidMeals = new List<Meal>();
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId);

            foreach (var line in mealsFile)
            {
                if (!line.StartsWith('\t'))
                {
                    currentMeal = new Meal
                    {
                        Date = ConvertToDate(line),
                        Name = RemoveCurlyBraces(line),
                        OwnerId = user.UserId
                    };
                    meals.Add(currentMeal);
                }
                else
                {
                    currentIngredient = new Ingredient
                    {
                        Name = line.Trim(),
                        OwnerId = user.UserId
                    };
                    currentMeal.Ingredients.Add(currentIngredient);
                }
            }

            invalidMeals.AddRange(meals.Where(m => m.Ingredients.Count < 1));
            meals.RemoveAll(meal => invalidMeals.Contains(meal));

            if (meals.Count > 0)
            {
                await _groceryAssistantRepository.ConvertMealsFileAsync(meals);
            }

            return meals;
        }


        public bool IsValidName(string name, int maxLength)
        {
            return name.Length <= maxLength;
        }


        public async Task<List<string>> CheckNamesFromConversion(string[] mealsFile, ulong discordId)
        {
            var message = new List<string>();
            var mealNames = new HashSet<string>();
            var savedMeals = await _groceryAssistantRepository.GetAllMealsAsync(discordId);
            var savedMealNames = new HashSet<string>(savedMeals.Select(meal => meal.Name));

            foreach (var line in mealsFile)
            {
                string name = line.Trim();
                name = RemoveCurlyBraces(name);

                if (!line.StartsWith('\t'))
                {
                    if (savedMealNames.Contains(name))
                    {
                        message.Add($"This meal is already saved in the database and can't be added a second time: {name}\n");
                    }
                    if (!mealNames.Add(name))
                    {
                        message.Add($"You have this meal listed twice in your meals file: {name}\n");
                    }
                    if (name.Length > 50)
                    {
                        message.Add($"Invalid meal name: {name}\n");
                    }
                }
                else
                {
                    if (name.Length > 51)
                    {
                        message.Add($"Invalid ingredient name: {name}\n");
                    }
                }
            }

            return message;
        }

        public async Task<bool> IsDuplicateNameAsync(string name, ulong discordId)
        {
            return await _groceryAssistantRepository.IsDuplicateNameAsync(name, discordId);
        }

        public static DateOnly? ConvertToDate(string mealName)
        {
            string pattern = @"\{(.*?)\}";
            var match = Regex.Match(mealName, pattern);
            if (match.Success)
            {
                string dateString = match.Groups[1].Value;
                if (DateOnly.TryParse(dateString, out DateOnly date))
                {
                    return date;
                }
            }
            return null;
        }

        public string RemoveCurlyBraces(string input)
        {
            return input.Split('{')[0].Trim();
        }


        public async Task<List<Meal>> ReplaceMealAsync(List<Meal> meals, int toReplace, ulong discordId)
        {
            var random = new Random();
            var allMeals = await _groceryAssistantRepository.GetAllMealsAsync(discordId);
            allMeals = allMeals.Where(m => !meals.Exists(m2 => m2.Name == m.Name)).ToList();
            var mealToAdd = allMeals.OrderBy(m => random.Next()).First();
            meals.RemoveAt(toReplace);
            meals.Add(mealToAdd);
            return meals;
        }

        public async Task<string> DownloadFileContentAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                return await httpClient.GetStringAsync(url);
            }
        }


        public List<string> SendLongMessage(List<string>? input = null, List<Meal>? meals = null, bool sendIngredients = false)
        {
            var response = new StringBuilder();
            var messages = new List<string>();
            int counter = 1;

            if (input != null)
            {
                foreach (var msg in input)
                {
                    if ((response.Length + msg.Length) > 2000)
                    {
                        messages.Add(response.ToString());
                        response.Clear();
                    }
                    response.AppendLine(msg);
                }
            }
            else if (meals != null)
            {
                foreach (var meal in meals)
                {
                    var mealDate = meal.Date?.ToString() ?? "Never";
                    string currentMeal;

                    if (sendIngredients)
                    {
                        currentMeal = $"{counter}. **{meal.Name}** (last used: **{mealDate}**): {string.Join(", ", meal.Ingredients.Select(i => i.Name))}\n";
                    }
                    else
                    {
                        currentMeal = $"{counter}. **{meal.Name}** (last used: **{mealDate}**)\n";
                    }

                    if ((response.Length + currentMeal.Length) > 2000)
                    {
                        messages.Add(response.ToString());
                        response.Clear();
                    }

                    response.Append(currentMeal);
                    counter++;
                }
            }

            if (response.Length > 0)
            {
                messages.Add(response.ToString());
            }

            return messages;
        }


        public void WriteSelectionFile(string filePath, IEnumerable<Meal> results)
        {
            var ingredientsList = new List<string>();
            using (StreamWriter writer = new(filePath))
            {
                writer.WriteLine("Meals:\n");
                foreach (var meal in results)
                {
                    writer.WriteLine($"{meal.Name}");
                    foreach (var ingredient in meal.Ingredients)
                    {
                        ingredientsList.Add(ingredient.Name);
                    }
                }

                writer.WriteLine("\n");
                writer.WriteLine("Ingredients:\n");

                ingredientsList.Sort();
                foreach (var ingredient in ingredientsList)
                {
                    writer.WriteLine($"{ingredient}");
                }
            }
        }

        public void WriteListFile(string filePath, IEnumerable<Meal> results)
        {
            int counter = 1;
            using (StreamWriter writer = new(filePath))
            {
                foreach (var meal in results)
                {
                    var mealDate = meal.Date?.ToString() ?? "Never";
                    writer.WriteLine($"{counter}. {meal.Name} (last used: {mealDate}): {string.Join(", ", meal.Ingredients.Select(i => i.Name))}\n");
                    counter++;
                }
            }
        }


        public string GetOutputPath(string fileName)
        {
            string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fullFilePath = Path.Combine(myDocumentsPath, fileName);
            return fullFilePath;
        }
    }
}