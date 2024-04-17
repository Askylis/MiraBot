using System;
using System.Collections.Generic;

namespace MeeraBot.DataAccess;

public partial class MealLog
{
    public int LogId { get; set; }

    public DateTime Date { get; set; }

    public int MealId { get; set; }

    public virtual Meal Meal { get; set; } = null!;
}
