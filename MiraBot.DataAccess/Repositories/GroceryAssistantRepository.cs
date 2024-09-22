using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiraBot.DataAccess.Repositories
{
    public class GroceryAssistantRepository : IGroceryAssistantRepository
    {
        private readonly DatabaseOptions _databaseOptions;
        private readonly UsersRepository _usersRepository;
        public GroceryAssistantRepository(IOptions<DatabaseOptions> databaseOptions, UsersRepository usersRepository)
        {
            _databaseOptions = databaseOptions.Value;
            _usersRepository = usersRepository;
        }

        public async Task AddMealAsync(string mealName, List<string> ingredients, ulong discordId, string? recipe, DateOnly? date)
        {
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var meal = new Meal
                {
                    Name = mealName,
                    OwnerId = user.UserId,
                    Date = date,
                    Ingredients = new List<Ingredient>(),
                    Recipe = recipe
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

        public async Task<Meal> FindAsync(int mealId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Meals.FirstOrDefaultAsync(m => m.MealId == mealId && !m.IsDeleted);
            }
        }

        public async Task DeleteMealAsync(int mealId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var meal = await context.Meals.FirstOrDefaultAsync(m => m.MealId == mealId);
                meal.IsDeleted = true;
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task EditMealAsync(Meal update)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var meal = context.Meals.Find(update.MealId);
                if (meal is null)
                {
                    return;
                }

                context.Entry(meal).CurrentValues.SetValues(update);
                if (!meal.Recipe.Any())
                {
                    meal.Recipe = update.Recipe;
                }
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<List<Meal>> GetAllMealsAsync(ulong discordId)
        {
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Meals
               .Include(m => m.Ingredients)
               .Where(m => m.OwnerId == user.UserId && !m.IsDeleted)
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
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return context.Meals.Count(m => m.OwnerId == user.UserId);
            }
        }

        public async Task<bool> IsDuplicateNameAsync(string name, ulong discordId)
        {
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var upperName = name.ToUpper();
                var count = await context.Meals
                    .CountAsync(m => m.Name.ToUpper() == upperName && m.OwnerId == user.UserId && !m.IsDeleted);
                return count > 0;
            }
        }
    }
}