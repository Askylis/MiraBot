using System;
using System.Collections.Generic;

namespace MiraBot.DataAccess;

public partial class Whitelist
{
    public int WhitelistId { get; set; }

    public int SenderId { get; set; }

    public int? RecipientId { get; set; }

    public virtual User? Recipient { get; set; }

    public virtual User Sender { get; set; } = null!;
}
