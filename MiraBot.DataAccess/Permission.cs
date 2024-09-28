namespace MiraBot.DataAccess;

public partial class Permission
{
    public int PermissionId { get; set; }

    public string Description { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
