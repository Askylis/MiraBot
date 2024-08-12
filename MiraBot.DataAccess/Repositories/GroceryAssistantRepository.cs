using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiraBot.DataAccess.Repositories
{
    public class GroceryAssistantRepository : IGroceryAssistantRepository
    {
        private readonly DatabaseOptions _databaseOptions;
        public GroceryAssistantRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            _databaseOptions = databaseOptions.Value;
        }

        public async Task AddMealAsync(string mealName, List<string> ingredients, string ownerName, DateOnly? date)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var meal = new Meal
                {
                    Name = mealName,
                    OwnerUserName = ownerName,
                    Date = date,
                    Ingredients = new List<Ingredient>()
                };

                foreach (var ingredientName in ingredients)
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

        public async Task DeleteMealAsync(int mealId, string ownerName)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var meal = await context.Meals
                .Include(m => m.Ingredients)
                .FirstOrDefaultAsync(m => m.MealId == mealId && m.OwnerUserName == ownerName);
                context.Meals.Remove(meal);
                await context.SaveChangesAsync();
            }
        }

        public async Task<bool> UserExistsAsync(User user)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return context.Users.Contains(user);
            }
        }

        public async Task AddNewUserAsync(User user)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Meal>> GetAllMealsAsync(string ownerName)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Meals
               .Include(m => m.Ingredients)
               .Where(m => m.OwnerUserName == ownerName)
               .ToListAsync();
            }
        }

        public async Task ConvertMealsFileAsync(List<Meal> meals)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                foreach (var meal in meals)
                {
                    context.Meals.Add(meal);
                }
                await context.SaveChangesAsync();
            }
        }

        public async Task<int> CountMealsByUserAsync(string ownerName)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return context.Meals.Count(m => m.OwnerUserName == ownerName);
            }
        }

        public async Task<bool> IsDuplicateNameAsync(string name, string ownerName)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var upperName = name.ToUpper();
                var count = await context.Meals
                    .CountAsync(m => m.Name.ToUpper() == upperName && m.OwnerUserName == ownerName);
                return count > 0;
            }
        }
    }
}