namespace SecureApi.Data;

using Microsoft.EntityFrameworkCore;
using SecureApi.Models;

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
    /// Gets or sets the Products table.
    /// </summary>
    public DbSet<Product> Products { get; set; } = null!;

    /// <summary>
    /// Gets or sets the RefreshTokens table.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

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

            // Role column configuration
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("User")
                .HasColumnType("TEXT");

            // Table name
            entity.ToTable("Users");
        });

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id)
                .HasName("PK_Product_Id");

            // Name column configuration
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            // Description column configuration
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("TEXT");

            // Price column configuration
            entity.Property(e => e.Price)
                .IsRequired()
                .HasColumnType("DECIMAL(18,2)");

            // Category column configuration
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            // StockQuantity column configuration
            entity.Property(e => e.StockQuantity)
                .IsRequired();

            // CreatedAt column configuration
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            // Table name
            entity.ToTable("Products");
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id)
                .HasName("PK_RefreshToken_Id");

            // Token column configuration
            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("TEXT");

            // Create a unique index on Token for fast lookups
            entity.HasIndex(e => e.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshToken_Token_Unique");

            // UserId foreign key
            entity.Property(e => e.UserId)
                .IsRequired();

            // ExpiresAt column configuration
            entity.Property(e => e.ExpiresAt)
                .IsRequired()
                .HasColumnType("DATETIME");

            // CreatedAt column configuration
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            // RevokedAt column configuration (nullable)
            entity.Property(e => e.RevokedAt)
                .IsRequired(false)
                .HasColumnType("DATETIME");

            // CreatedByIp column configuration
            entity.Property(e => e.CreatedByIp)
                .IsRequired()
                .HasMaxLength(45)
                .HasColumnType("TEXT");

            // Relationship with User (one user can have many refresh tokens)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RefreshToken_User_UserId");

            // Index on UserId for finding tokens by user
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_RefreshToken_UserId");

            // Index on ExpiresAt for cleanup queries (finding expired tokens)
            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("IX_RefreshToken_ExpiresAt");

            // Table name
            entity.ToTable("RefreshTokens");
        });
    }
}
