﻿using Microsoft.EntityFrameworkCore;

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

    public virtual DbSet<Blacklist> Blacklists { get; set; }

    public virtual DbSet<Bug> Bugs { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Meal> Meals { get; set; }

    public virtual DbSet<MealLog> MealLogs { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Reminder> Reminders { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Whitelist> Whitelists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(_connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blacklist>(entity =>
        {
            entity.Property(e => e.BlacklistId).HasColumnName("BlacklistID");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.BlacklistRecipientUsers)
                .HasForeignKey(d => d.RecipientUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Blacklists_Recipients");

            entity.HasOne(d => d.SenderUser).WithMany(p => p.BlacklistSenderUsers)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Blacklists_Senders");
        });

        modelBuilder.Entity<Bug>(entity =>
        {
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.HowToReproduce)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Severity)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.Bugs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bugs_Users");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Owner).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ingredients_Users");
        });

        modelBuilder.Entity<Meal>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Recipe).IsUnicode(false);

            entity.HasOne(d => d.Owner).WithMany(p => p.Meals)
                .HasForeignKey(d => d.OwnerId)
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
            entity.Property(e => e.Name)
                .HasMaxLength(32)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.Property(e => e.Message)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasMany(d => d.Users).WithMany(p => p.Reminders)
                .UsingEntity<Dictionary<string, object>>(
                    "ReminderRecipient",
                    r => r.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ReminderRecipients_Users"),
                    l => l.HasOne<Reminder>().WithMany()
                        .HasForeignKey("ReminderId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ReminderRecipients_Reminders"),
                    j =>
                    {
                        j.HasKey("ReminderId", "UserId");
                        j.ToTable("ReminderRecipients");
                    });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_Users_1");

            entity.HasIndex(e => e.DiscordId, "IX_Users").IsUnique();

            entity.Property(e => e.Nickname)
                .HasMaxLength(32)
                .IsUnicode(false);
            entity.Property(e => e.Timezone)
                .HasMaxLength(75)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(32)
                .IsUnicode(false);
            entity.Property(e => e.DiscordId)
                .HasConversion(
                    v => (long)v,
                    v => (ulong)v);


            entity.HasMany(d => d.Permissions).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserPermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserPermissions_Permissions"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserPermissions_Users"),
                    j =>
                    {
                        j.HasKey("UserId", "PermissionId");
                        j.ToTable("UserPermissions");
                    });
        });

        modelBuilder.Entity<Whitelist>(entity =>
        {
            entity.HasOne(d => d.RecipientUser).WithMany(p => p.WhitelistRecipientUsers)
                .HasForeignKey(d => d.RecipientUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Whitelists_Recipients");

            entity.HasOne(d => d.SenderUser).WithMany(p => p.WhitelistSenderUsers)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Whitelists_Senders");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
