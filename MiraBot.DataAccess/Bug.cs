using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class Bug
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Description { get; set; } = null!;

    public string HowToReproduce { get; set; } = null!;

    public string Severity { get; set; } = null!;

    public DateTime DateTime { get; set; }

    public bool IsFixed { get; set; }

    public virtual User User { get; set; } = null!;
}
