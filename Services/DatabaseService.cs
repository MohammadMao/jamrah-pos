using JamrahPOS.Data;
using JamrahPOS.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace JamrahPOS.Services
{
    /// <summary>
    /// Service for initializing and managing the database
    /// </summary>
    public class DatabaseService
    {
        private readonly PosDbContext _context;

        public DatabaseService()
        {
            _context = new PosDbContext();
        }

        /// <summary>
        /// Initializes the database (creates if not exists and applies migrations)
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                Console.WriteLine("[DB] Starting database initialization...");

                // Use migrations to create/update database schema
                Console.WriteLine("[DB] Applying migrations...");
                await _context.Database.MigrateAsync();
                Console.WriteLine("[DB] Migrations applied successfully");

                // Seed initial data
                await SeedDataAsync();

                Console.WriteLine("[DB] Database initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] ERROR initializing database: {ex.Message}");
                Console.WriteLine($"[DB] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Seeds initial data into the database
        /// </summary>
        private async Task SeedDataAsync()
        {
            try
            {
                // Check if admin user exists
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                
                if (adminUser == null)
                {
                    Console.WriteLine("[DB] Admin user not found. Creating admin user...");

                    // Generate a fresh BCrypt hash for "admin123"
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 11);
                    Console.WriteLine($"[DB] Generated password hash: {passwordHash}");

                    // Create admin user
                    adminUser = new Models.User
                    {
                        Username = "admin",
                        PasswordHash = passwordHash,
                        Role = "Admin",
                        IsActive = true
                    };

                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("[DB] Admin user created successfully");
                }
                else
                {
                    Console.WriteLine("[DB] Admin user already exists. Skipping.");
                }

                // Check if categories exist, if not seed them
                var categoriesExist = await _context.Categories.AnyAsync();
                if (!categoriesExist)
                {
                    Console.WriteLine("[DB] No categories found. Seeding categories...");
                    // Seed categories
                    var categories = new[]
                    {
                        new Models.Category { Name = "بيرقر", IsActive = true },         // Burger
                        new Models.Category { Name = "المشروبات", IsActive = true },     // Drinks
                        new Models.Category { Name = "الطلبات", IsActive = true },       // Orders/Dishes
                        new Models.Category { Name = "السندوتشات", IsActive = true },    // Sandwiches
                        new Models.Category { Name = "الحلويات", IsActive = true }       // Desserts
                    };

                    _context.Categories.AddRange(categories);
                    Console.WriteLine("[DB] Added sample categories");
                    
                    // Save all changes
                    int saved = await _context.SaveChangesAsync();
                    Console.WriteLine($"[DB] Saved {saved} entities to database");
                }
                else
                {
                    Console.WriteLine("[DB] Categories already exist. Skipping seed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] ERROR seeding data: {ex.Message}");
                Console.WriteLine($"[DB] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Gets the database context
        /// </summary>
        public PosDbContext GetContext()
        {
            return _context;
        }

        /// <summary>
        /// Closes the database connection
        /// </summary>
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
