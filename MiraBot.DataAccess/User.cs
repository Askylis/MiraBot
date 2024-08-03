using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class User
{
    public string UserName { get; set; } = null!;

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
