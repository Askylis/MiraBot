﻿namespace MiraBot.DataAccess.Repositories
{
    public interface IGroceryAssistantRepository
    {
        Task AddMealAsync(string mealName, List<string> ingredients, ulong discordId, string? recipe, DateOnly? date);
        Task<List<Meal>> GetAllMealsAsync(ulong discordId);
        Task ConvertMealsFileAsync(List<Meal> meals);
        Task DeleteMealAsync(int mealId);
        Task<int> CountMealsByUserAsync(ulong discordId);
        Task<bool> IsDuplicateNameAsync(string name, ulong discordId);
        Task EditMealAsync(Meal update);
        Task<Meal> FindAsync(int mealId);
    }
}
