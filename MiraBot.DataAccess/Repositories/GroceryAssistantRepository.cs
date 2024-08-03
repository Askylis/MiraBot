using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiraBot.DataAccess.Repositories
{
    public class GroceryAssistantRepository : IGroceryAssistantRepository
    {
        private readonly DatabaseOptions databaseOptions;
        public GroceryAssistantRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            this.databaseOptions = databaseOptions.Value;
        }

        public async Task AddIngredientAsync(string name, string ownerName)
        {
            using (var context = new MiraBotContext(databaseOptions.ConnectionString))
            {
                var ingredient = new Ingredient();
                ingredient.Name = name;
                ingredient.OwnerUserName = ownerName;
                context.Ingredients.Add(ingredient);
                await context.SaveChangesAsync();
            }
        }

        public async Task AddMealAsync(string mealName, List<string> ingredientNames, string ownerName, DateOnly? date)
        {
            using (var context = new MiraBotContext(databaseOptions.ConnectionString))
            {
                var meal = new Meal
                {
                    Name = mealName,
                    OwnerUserName = ownerName,
                    Date = date,
                    Ingredients = new List<Ingredient>()
            };

                    foreach (var ingredientName in ingredientNames)
                    {
                        var newIngredient = new Ingredient
                        {
                            Name = ingredientName,
                            OwnerUserName = ownerName
                        };
                        meal.Ingredients.Add(newIngredient);
                    }
     
                context.Meals.Add(meal);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteMealAsync(string mealName, string ownerName)
        {
            using (var context = new MiraBotContext(databaseOptions.ConnectionString))
            {
                var meal = await context.Meals
                .Include(m => m.Ingredients)
                .FirstOrDefaultAsync(m => m.Name == mealName && m.OwnerUserName == ownerName);
                context.Meals.Remove(meal);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Meal>> GetAllMealsAsync(string ownerName)
        {
            using (var context = new MiraBotContext(databaseOptions.ConnectionString))
            {
                return await context.Meals
               .Include(m => m.Ingredients)
               .Where(m => m.OwnerUserName == ownerName)
               .ToListAsync();
            }
        }

        public async Task ConvertMealsFileAsync(List<Meal> meals, string ownerName)
        {
            using (var context = new MiraBotContext(databaseOptions.ConnectionString))
            {
                foreach (var meal in meals)
                {
                    context.Meals.Add(meal);
                }
                await context.SaveChangesAsync();
            }
        }

        public async Task<Meal> GetMealByNameAsync(string name, string ownerName)
        {
            using (var context = new MiraBotContext(databaseOptions.ConnectionString))
            {
                return await context.Meals
                .FirstOrDefaultAsync(m => m.Name == name && m.OwnerUserName == ownerName);
            }
        }
    }
}
