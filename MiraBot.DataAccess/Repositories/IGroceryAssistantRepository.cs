namespace MiraBot.DataAccess.Repositories
{
    public interface IGroceryAssistantRepository
    {
        Task AddMealAsync(string mealName, List<string> ingredients, string ownerName, DateOnly? date);
        Task<List<Meal>> GetAllMealsAsync(string ownerName);
        Task ConvertMealsFileAsync(List<Meal> meals);
        Task DeleteMealAsync(int mealId, string ownerName);
        Task<int> CountMealsByUserAsync(string ownerName);
        Task<bool> IsDuplicateNameAsync(string name, string ownerName);
        Task<bool> UserExistsAsync(User user);
        Task AddNewUserAsync(User user);
    }
}
