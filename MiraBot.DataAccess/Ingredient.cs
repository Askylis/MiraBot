using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class Ingredient
{
    public int IngredientId { get; set; }

    public int OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public virtual User IngredientNavigation { get; set; } = null!;

    public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();
}
