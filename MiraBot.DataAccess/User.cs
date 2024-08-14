using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class User
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public ulong DiscordId { get; set; }

    public string? Timezone { get; set; }

    public string? Nickname { get; set; }

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();

    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
