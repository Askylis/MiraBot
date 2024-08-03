namespace MiraBot.DataAccess.Repositories
{
    public interface IGroceryAssistantRepository
    {
        Task AddIngredientAsync(string ingredientName, string ownerName);
        Task AddMealAsync(string mealName, List<string> ingredients, string ownerName, DateOnly? date);
        Task<List<Meal>> GetAllMealsAsync(string ownerName);
        Task ConvertMealsFileAsync(List<Meal> meals, string ownerName);
        Task<Meal> GetMealByNameAsync(string name, string ownerName);
        Task DeleteMealAsync(string mealName, string ownerName);
    }
}
