using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class Blacklist
{
    public int BlacklistId { get; set; }

    public int SenderUserId { get; set; }

    public int RecipientUserId { get; set; }

    public virtual User RecipientUser { get; set; } = null!;

    public virtual User SenderUser { get; set; } = null!;
}
