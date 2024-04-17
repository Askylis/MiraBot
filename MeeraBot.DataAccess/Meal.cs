using System;
using System.Collections.Generic;

namespace MeeraBot.DataAccess;

public partial class Meal
{
    public int MealId { get; set; }

    public string Name { get; set; } = null!;

    public string OwnerUserName { get; set; } = null!;

    public virtual ICollection<MealLog> MealLogs { get; set; } = new List<MealLog>();

    public virtual User OwnerUserNameNavigation { get; set; } = null!;

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}
