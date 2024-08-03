using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class Meal
{
    public int MealId { get; set; }

    public string Name { get; set; } = null!;

    public string OwnerUserName { get; set; } = null!;

    public DateOnly? Date { get; set; }

    public virtual ICollection<MealLog> MealLogs { get; set; } = new List<MealLog>();

    public virtual User OwnerUserNameNavigation { get; set; } = null!;

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}
