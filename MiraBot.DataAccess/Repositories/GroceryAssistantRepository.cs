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

        public async Task AddMealAsync(string mealName, List<string> ingredients, ulong discordId, DateOnly? date)
        {
            var user = await GetUserByDiscordId(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var meal = new Meal
                {
                    Name = mealName,
                    OwnerId = user.UserId,
                    Date = date,
                    Ingredients = new List<Ingredient>()
                };

                foreach (var ingredientName in ingredients)
                {
                    var newIngredient = new Ingredient
                    {
                        Name = ingredientName,
                        OwnerId = user.UserId
                    };
                    meal.Ingredients.Add(newIngredient);
                }

                context.Meals.Add(meal);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteMealAsync(int mealId, ulong discordId)
        {
            var user = await GetUserByDiscordId(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var meal = await context.Meals
                .Include(m => m.Ingredients)
                .FirstOrDefaultAsync(m => m.MealId == mealId && m.OwnerId == user.UserId);
                context.Meals.Remove(meal);
                await context.SaveChangesAsync();
            }
        }

        public async Task<bool> UserExistsAsync(ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Users.AnyAsync(u => u.DiscordId == discordId);
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

        public async Task<User> GetUserByDiscordId(ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await GetUserByDiscordId(discordId, context);
            }
        }

        private async Task<User> GetUserByDiscordId(ulong discordId, MiraBotContext ctx)
        {
            return await ctx.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);
        }

        public async Task<List<Meal>> GetAllMealsAsync(ulong discordId)
        {
            var user = await GetUserByDiscordId(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Meals
               .Include(m => m.Ingredients)
               .Where(m => m.OwnerId == user.UserId)
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

        public async Task<int> CountMealsByUserAsync(ulong discordId)
        {
            var user = await GetUserByDiscordId(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return context.Meals.Count(m => m.OwnerId == user.UserId);
            }
        }

        public async Task<bool> IsDuplicateNameAsync(string name, ulong discordId)
        {
            var user = await GetUserByDiscordId(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var upperName = name.ToUpper();
                var count = await context.Meals
                    .CountAsync(m => m.Name.ToUpper() == upperName && m.OwnerId == user.UserId);
                return count > 0;
            }
        }
    }
}