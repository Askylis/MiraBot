using System;
using System.Collections.Generic;

namespace MeeraBot.DataAccess;

public partial class Permission
{
    public int PermissionId { get; set; }

    public string Description { get; set; } = null!;

    public virtual ICollection<User> UserNames { get; set; } = new List<User>();
}
