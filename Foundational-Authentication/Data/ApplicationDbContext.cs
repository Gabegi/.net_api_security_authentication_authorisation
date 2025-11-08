namespace Foundational_Authentication.Data;

using Microsoft.EntityFrameworkCore;
using Foundational_Authentication.Models;

/// <summary>
/// Application database context for Entity Framework Core.
/// Manages all database operations and entity configurations.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users table.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Configures the model and applies constraints, relationships, and indexes.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id)
                .HasName("PK_User_Id");

            // Email column configuration
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(254)
                .HasColumnType("TEXT");

            // Create a unique index on Email to ensure no duplicate emails
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_User_Email_Unique");

            // PasswordHash column configuration
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(512)
                .HasColumnType("TEXT");

            // FullName column configuration
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            // CreatedAt column configuration
            // Database-level default for server-side timestamp generation
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd(); // Ensures EF Core doesn't try to update this value

            // LastLoginAt column configuration (nullable)
            entity.Property(e => e.LastLoginAt)
                .IsRequired(false)
                .HasColumnType("DATETIME");

            // Index on LastLoginAt for analytics and reporting queries
            entity.HasIndex(e => e.LastLoginAt)
                .HasDatabaseName("IX_User_LastLoginAt");

            // Table name
            entity.ToTable("Users");
        });
    }
}
