using Microsoft.EntityFrameworkCore;

namespace MiraBot.DataAccess;

public partial class MiraBotContext : DbContext
{
    private readonly string _connectionString;

    public MiraBotContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public MiraBotContext(DbContextOptions<MiraBotContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Meal> Meals { get; set; }

    public virtual DbSet<MealLog> MealLogs { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer(_connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OwnerUserName)
                .HasMaxLength(32)
                .IsUnicode(false);

            entity.HasOne(d => d.OwnerUserNameNavigation).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.OwnerUserName)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Ingredients_Users");
        });

        modelBuilder.Entity<Meal>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OwnerUserName)
                .HasMaxLength(32)
                .IsUnicode(false);

            entity.HasOne(d => d.OwnerUserNameNavigation).WithMany(p => p.Meals)
                .HasForeignKey(d => d.OwnerUserName)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meals_Users");

            entity.HasMany(d => d.Ingredients).WithMany(p => p.Meals)
                .UsingEntity<Dictionary<string, object>>(
                    "IngredientsInMeal",
                    r => r.HasOne<Ingredient>().WithMany()
                        .HasForeignKey("IngredientId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_IngredientsInMeals_Ingredients"),
                    l => l.HasOne<Meal>().WithMany()
                        .HasForeignKey("MealId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("FK_IngredientsInMeals_IngredientsInMeals"),
                    j =>
                    {
                        j.HasKey("MealId", "IngredientId");
                        j.ToTable("IngredientsInMeals");
                    });
        });

        modelBuilder.Entity<MealLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("MealLog");

            entity.HasOne(d => d.Meal).WithMany(p => p.MealLogs)
                .HasForeignKey(d => d.MealId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealLog_Meals");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserName);

            entity.Property(e => e.UserName)
                .HasMaxLength(32)
                .IsUnicode(false);

            entity.HasMany(d => d.Permissions).WithMany(p => p.UserNames)
                .UsingEntity<Dictionary<string, object>>(
                    "UserPermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserPermissions_Permissions"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserName")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserPermissions_Users"),
                    j =>
                    {
                        j.HasKey("UserName", "PermissionId");
                        j.ToTable("UserPermissions");
                        j.IndexerProperty<string>("UserName")
                            .HasMaxLength(32)
                            .IsUnicode(false);
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
