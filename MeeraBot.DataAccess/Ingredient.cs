using System;
using System.Collections.Generic;

namespace MeeraBot.DataAccess;

public partial class Ingredient
{
    public int IngredientId { get; set; }

    public string Name { get; set; } = null!;

    public string OwnerUserName { get; set; } = null!;

    public virtual User OwnerUserNameNavigation { get; set; } = null!;

    public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();
}
