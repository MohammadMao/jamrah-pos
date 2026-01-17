using JamrahPOS.Services;

namespace JamrahPOS.Helpers
{
    /// <summary>
    /// Helper class for role-based authorization checks
    /// </summary>
    public static class AuthorizationHelper
    {
        /// <summary>
        /// Checks if the current user has admin privileges
        /// </summary>
        public static bool IsAdmin()
        {
            return SessionService.Instance.IsAdmin;
        }

        /// <summary>
        /// Checks if the current user has cashier privileges
        /// </summary>
        public static bool IsCashier()
        {
            return SessionService.Instance.IsCashier;
        }

        /// <summary>
        /// Checks if the current user is logged in
        /// </summary>
        public static bool IsAuthenticated()
        {
            return SessionService.Instance.IsLoggedIn;
        }

        /// <summary>
        /// Requires admin role, returns true if user is admin, false otherwise
        /// Shows error message if user doesn't have permission
        /// </summary>
        public static bool RequireAdmin(string feature = "هذه الميزة")
        {
            if (!IsAdmin())
            {
                System.Windows.MessageBox.Show(
                    $"{feature} متاحة للمدير فقط",
                    "غير مصرح",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the current user's display name
        /// </summary>
        public static string GetCurrentUserName()
        {
            return SessionService.Instance.CurrentUser?.Username ?? "غير معروف";
        }

        /// <summary>
        /// Gets the current user's role in Arabic
        /// </summary>
        public static string GetCurrentUserRoleArabic()
        {
            var role = SessionService.Instance.CurrentUser?.Role;
            return role switch
            {
                UserRoles.Admin => "مدير",
                UserRoles.Cashier => "كاشير",
                _ => "غير معروف"
            };
        }
    }
}
