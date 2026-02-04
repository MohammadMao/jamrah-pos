using Microsoft.EntityFrameworkCore;
using JamrahPOS.Models;
using System.IO;

namespace JamrahPOS.Data
{
    /// <summary>
    /// Database context for the Jamrah POS system
    /// </summary>
    public class PosDbContext : DbContext
    {
        // DbSets for all entities
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }

        public PosDbContext()
        {
        }

        public PosDbContext(DbContextOptions<PosDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Get the application data folder path
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dbFolder = Path.Combine(appDataPath, "JamrahPOS");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(dbFolder))
                {
                    Directory.CreateDirectory(dbFolder);
                }

                string dbPath = Path.Combine(dbFolder, "JamrahPOS.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IsActive).IsRequired();

                // Index on Username for faster lookups
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure Category entity
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

            // Configure MenuItem entity
            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.IsActive).IsRequired();

                // Define relationship with Category
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.MenuItems)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OrderDateTime).IsRequired();
                entity.Property(e => e.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IsVoided).IsRequired();

                // Index on OrderNumber for faster lookups
                entity.HasIndex(e => e.OrderNumber).IsUnique();

                // Define relationship with User (Cashier)
                entity.HasOne(e => e.Cashier)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(e => e.CashierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).IsRequired().HasColumnType("decimal(18,2)");

                // Define relationship with Order
                entity.HasOne(e => e.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Define relationship with MenuItem
                entity.HasOne(e => e.MenuItem)
                      .WithMany(m => m.OrderItems)
                      .HasForeignKey(e => e.MenuItemId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Seeds initial data into the database
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default admin user with hashed password
            // Username: admin, Password: admin123
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "$2a$11$8K1p/a0dL3LHAkH4v.9tCe8sJlUgz1qJ3x5t6ygWdZKvIYZp3qYXK", // BCrypt hash of "admin123"
                    Role = "Admin",
                    IsActive = true
                }
            );

            // Seed sample categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "المشروبات" }, // Drinks
                new Category { Id = 2, Name = "المقبلات" },   // Appetizers
                new Category { Id = 3, Name = "الأطباق الرئيسية" }, // Main Dishes
                new Category { Id = 4, Name = "الحلويات" }    // Desserts
            );
        }
    }
}
