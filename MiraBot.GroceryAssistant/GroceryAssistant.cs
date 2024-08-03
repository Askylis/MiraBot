using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System.Text;
using System.Text.RegularExpressions;

namespace MiraBot.GroceryAssistance
{
    public class GroceryAssistant
    {
        private readonly IGroceryAssistantRepository groceryAssistantRepository;

        public GroceryAssistant(IGroceryAssistantRepository groceryAssistantRepository)
        {
            this.groceryAssistantRepository = groceryAssistantRepository;
        }

        public async Task AddIngredientAsync(string name, string ownerName)
        {
            await this.groceryAssistantRepository.AddIngredientAsync(name, ownerName);
        }

        public async Task AddMealAsync(string mealName, List<string> ingredients, string ownerName, DateOnly? date = null)
        {
            await this.groceryAssistantRepository.AddMealAsync(mealName, ingredients, ownerName, date);
        }

        public async Task DeleteMealAsync(string mealName, string ownerName)
        {
            await groceryAssistantRepository.DeleteMealAsync(mealName, ownerName);
        }

        public async Task<List<Meal>> GetAllMealsAsync(string ownerName)
        {
            return await groceryAssistantRepository.GetAllMealsAsync(ownerName);
        }

        public IEnumerable<string> TrimIngredients(string ingredientsInput)
        {
            string[] ingredients = ingredientsInput.Split(",");
            var trimmedIngredients = ingredients.Select(i => i.Trim());
            return trimmedIngredients;
        }

        public async Task<List<Meal>> GenerateMealIdeasAsync(int numberOfMeals, string ownerName)
        {
            Random random = new();
            var allMeals = await groceryAssistantRepository.GetAllMealsAsync(ownerName);
            var randomizedMeals = allMeals.OrderBy(m => random.Next()).Take(numberOfMeals);
            return randomizedMeals.ToList();
        }

        public async Task<bool> CheckForValidNumberAsync(int number, string ownerName, bool allowZero)
        {
            if ((!allowZero && number <= 0) || (allowZero && number < 0)) 
            {
                return false;
            }
            var allMeals = await groceryAssistantRepository.GetAllMealsAsync(ownerName);
            return number <= allMeals.Count();
        }

        public async Task<List<Meal>> ConvertMealsFileAsync(string[] mealsFile, string ownerName)
        {
            var meals = new List<Meal>();
            var currentMeal = new Meal();
            Ingredient currentIngredient;
            var invalidMeals = new List<Meal>();

            foreach (var line in mealsFile)
            {
                if (!line.StartsWith('\t'))
                {
                    currentMeal = new Meal();
                    currentMeal.Date = ConvertToDate(line);
                    currentMeal.Name = RemoveCurlyBraces(line);
                    currentMeal.OwnerUserName = ownerName;
                    meals.Add(currentMeal);
                }
                else
                {
                    currentIngredient = new Ingredient();
                    currentIngredient.Name = line.Trim();
                    currentIngredient.OwnerUserName = ownerName;
                    currentMeal.Ingredients.Add(currentIngredient);
                }
            }

            foreach (var meal in meals)
            {
                if (meal.Ingredients.Count() < 1)
                {
                    invalidMeals.Add(meal);
                }
            }

            if (invalidMeals.Count() > 0)
            {
                foreach (var meal in invalidMeals)
                {
                    meals.Remove(meal);
                }
            }
            
            if (meals.Count > 0)
            {
                await groceryAssistantRepository.ConvertMealsFileAsync(meals, ownerName);
            }

            return meals;
        }

        public Ingredient UpdateIngredient(string name, string ownerName)
        {
            var ingredient = new Ingredient();
            ingredient.Name = name;
            ingredient.OwnerUserName = ownerName;
            return ingredient;
        }


        public bool CheckForInvalidName(string name, int maxLength)
        {
            return name.Length <= maxLength;
        }


        public async Task<List<string>> CheckNamesFromConversion(string[] mealsFile, string ownerName)
        {
            var message = new List<string>();
            var mealNames = new HashSet<string>();
            var savedMeals = await groceryAssistantRepository.GetAllMealsAsync(ownerName);
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

        public async Task<bool> HasDuplicateNameAsync(string name, string ownerName)
        {
            var meals = await groceryAssistantRepository.GetAllMealsAsync(ownerName);
            return meals.Exists(x => x.Name.ToUpperInvariant() == name.ToUpperInvariant());  
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


        public async Task<List<Meal>> ReplaceMealAsync(List<Meal> meals, int toReplace, string ownerName)
        {
            var random = new Random();
            var allMeals = await groceryAssistantRepository.GetAllMealsAsync(ownerName);
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


        public List<string> SendLongMessage(List<string> input = null, List<Meal> meals = null, bool sendIngredients = false)
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
            using (StreamWriter writer = new (filePath))
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
            using (StreamWriter writer = new (filePath))
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
