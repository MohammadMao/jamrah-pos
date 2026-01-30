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

                // Ensure database is created
                bool created = await _context.Database.EnsureCreatedAsync();
                Console.WriteLine($"[DB] Database created/exists: {created}");

                // Seed initial data if database was just created
                await SeedDataAsync();

                // Alternative: Use migrations (uncomment if using migrations)
                // await _context.Database.MigrateAsync();

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
                // Check if admin user exists
                var adminExists = await _context.Users.AnyAsync(u => u.Username == "admin");
                if (adminExists)
                {
                    Console.WriteLine("[DB] Admin user already exists. Verifying password...");
                    var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                    if (existingAdmin != null)
                    {
                        bool passwordMatches = AuthenticationService.VerifyPassword("admin123", existingAdmin.PasswordHash);
                        Console.WriteLine($"[DB] Existing admin password matches default: {passwordMatches}");
                        if (!passwordMatches)
                        {
                            Console.WriteLine("[DB] Updating admin password hash to default 'admin123'");
                            existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 11);
                            _context.Users.Update(existingAdmin);
                            await _context.SaveChangesAsync();
                            Console.WriteLine("[DB] Admin password updated");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[DB] Seeding initial data...");

                    // Generate a fresh BCrypt hash for "admin123"
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 11);
                    Console.WriteLine($"[DB] Generated password hash: {passwordHash}");

                    // Seed admin user
                    var adminUser = new Models.User
                    {
                        Id = 1,
                        Username = "admin",
                        PasswordHash = passwordHash,
                        Role = "Admin",
                        IsActive = true
                    };

                    _context.Users.Add(adminUser);
                    Console.WriteLine("[DB] Added admin user");

                // Seed categories
                var categories = new[]
                {
                    new Models.Category { Id = 1, Name = "المشروبات" },     // Drinks
                    new Models.Category { Id = 2, Name = "المقبلات" },      // Appetizers
                    new Models.Category { Id = 3, Name = "الأطباق الرئيسية" }, // Main Dishes
                    new Models.Category { Id = 4, Name = "الحلويات" }       // Desserts
                };

                _context.Categories.AddRange(categories);
                Console.WriteLine("[DB] Added sample categories");

                // Save all changes
                int saved = await _context.SaveChangesAsync();
                Console.WriteLine($"[DB] Saved {saved} entities to database");
            }}
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
