using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class Reminder
{
    public int ReminderId { get; set; }

    public string OwnerName { get; set; } = null!;

    public string RecipientName { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsCompleted { get; set; }

    public DateTime DateTime { get; set; }
}
