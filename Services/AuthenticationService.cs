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
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            // Find user by username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
            {
                return null;
            }

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash))
            {
                return null;
            }

            return user;
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
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
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
