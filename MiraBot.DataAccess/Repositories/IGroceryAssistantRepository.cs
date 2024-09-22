namespace MiraBot.DataAccess.Repositories
{
    public interface IGroceryAssistantRepository
    {
        Task AddMealAsync(string mealName, List<string> ingredients, ulong discordId, DateOnly? date);
        Task<List<Meal>> GetAllMealsAsync(ulong discordId);
        Task ConvertMealsFileAsync(List<Meal> meals);
        Task DeleteMealAsync(int mealId, ulong discordId);
        Task<int> CountMealsByUserAsync(ulong discordId);
        Task<bool> IsDuplicateNameAsync(string name, ulong discordId);
        Task EditMealAsync(Meal update);
        Task<Meal> FindAsync(int mealId);
    }
}
