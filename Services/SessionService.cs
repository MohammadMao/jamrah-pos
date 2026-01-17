using JamrahPOS.Models;

namespace JamrahPOS.Services
{
    /// <summary>
    /// Service for managing the current user session
    /// </summary>
    public class SessionService
    {
        private static SessionService? _instance;
        private static readonly object _lock = new object();

        public User? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;
        public bool IsAdmin => CurrentUser?.Role == Helpers.UserRoles.Admin;
        public bool IsCashier => CurrentUser?.Role == Helpers.UserRoles.Cashier;

        // Singleton pattern
        public static SessionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SessionService();
                        }
                    }
                }
                return _instance;
            }
        }

        private SessionService()
        {
        }

        /// <summary>
        /// Sets the current logged-in user
        /// </summary>
        public void Login(User user)
        {
            CurrentUser = user;
        }

        /// <summary>
        /// Clears the current user session
        /// </summary>
        public void Logout()
        {
            CurrentUser = null;
        }

        /// <summary>
        /// Checks if the current user has the specified role
        /// </summary>
        public bool HasRole(string role)
        {
            return CurrentUser?.Role == role;
        }

        /// <summary>
        /// Checks if the current user has admin privileges
        /// </summary>
        public bool RequireAdmin()
        {
            return IsAdmin;
        }
    }
}
