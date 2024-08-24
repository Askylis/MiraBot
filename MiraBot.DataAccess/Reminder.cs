using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class Reminder
{
    public int ReminderId { get; set; }

    public int OwnerId { get; set; }

    public int RecipientId { get; set; }

    public string Message { get; set; } = null!;

    public bool IsCompleted { get; set; }

    public DateTime DateTime { get; set; }

    public bool IsRecurring { get; set; }

    public int InSeconds { get; set; }

    public int InMinutes { get; set; }

    public int InHours { get; set; }

    public int InDays { get; set; }

    public int InWeeks { get; set; }

    public int InMonths { get; set; }

    public int InYears { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
