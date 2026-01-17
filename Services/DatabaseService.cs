using JamrahPOS.Data;
using Microsoft.EntityFrameworkCore;

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
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Alternative: Use migrations (uncomment if using migrations)
                // await _context.Database.MigrateAsync();

                Console.WriteLine("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
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
