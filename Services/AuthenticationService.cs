using JamrahPOS.Data;
using JamrahPOS.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace JamrahPOS.Services
{
    /// <summary>
    /// Service for handling user authentication
    /// </summary>
    public class AuthenticationService
    {
        private readonly PosDbContext _context;

        public AuthenticationService(PosDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Authenticates a user with username and password
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Plain text password</param>
        /// <returns>User object if authentication succeeds, null otherwise</returns>
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            Console.WriteLine($"[AUTH] AuthenticateAsync called with username: {username}");
            
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("[AUTH] Username or password is empty");
                return null;
            }

            try
            {
                Console.WriteLine("[AUTH] Querying database for user...");
                // Find user by username
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user == null)
                {
                    Console.WriteLine($"[AUTH] User '{username}' not found in database");
                    // List all users for debugging
                    var allUsers = await _context.Users.ToListAsync();
                    Console.WriteLine($"[AUTH] Total users in database: {allUsers.Count}");
                    foreach (var u in allUsers)
                    {
                        Console.WriteLine($"[AUTH]   - Username: {u.Username}, IsActive: {u.IsActive}");
                    }
                    return null;
                }

                Console.WriteLine($"[AUTH] User found: {user.Username}, IsActive: {user.IsActive}");

                // Verify password
                bool passwordValid = VerifyPassword(password, user.PasswordHash);
                Console.WriteLine($"[AUTH] Password verification result: {passwordValid}");
                
                if (!passwordValid)
                {
                    Console.WriteLine("[AUTH] Password verification failed");
                    return null;
                }

                Console.WriteLine($"[AUTH] Authentication successful for user: {user.Username}");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH] EXCEPTION in AuthenticateAsync: {ex.Message}");
                Console.WriteLine($"[AUTH] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Hashes a password using BCrypt
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password</returns>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="hash">Hashed password</param>
        /// <returns>True if password matches, false otherwise</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            try
            {
                Console.WriteLine($"[AUTH] VerifyPassword called");
                Console.WriteLine($"[AUTH] Password to verify: {password}");
                Console.WriteLine($"[AUTH] Hash from DB: {hash}");
                
                bool result = BCrypt.Net.BCrypt.Verify(password, hash);
                Console.WriteLine($"[AUTH] BCrypt.Verify result: {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH] EXCEPTION in VerifyPassword: {ex.Message}");
                Console.WriteLine($"[AUTH] Exception type: {ex.GetType().Name}");
                return false;
            }
        }

        /// <summary>
        /// Changes a user's password
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="oldPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>True if password was changed successfully</returns>
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify old password
            if (!VerifyPassword(oldPassword, user.PasswordHash))
            {
                return false;
            }

            // Hash and save new password
            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Validates if a user has admin privileges
        /// </summary>
        public bool IsAdmin(User user)
        {
            return user.Role == Helpers.UserRoles.Admin;
        }

        /// <summary>
        /// Validates if a user has cashier privileges
        /// </summary>
        public bool IsCashier(User user)
        {
            return user.Role == Helpers.UserRoles.Cashier;
        }
    }
}
