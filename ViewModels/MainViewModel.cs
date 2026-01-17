using System.Windows;
using System.Windows.Input;
using JamrahPOS.Helpers;
using JamrahPOS.Services;

namespace JamrahPOS.ViewModels
{
    /// <summary>
    /// ViewModel for the MainWindow
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private string _title = "نظام نقاط البيع - Jamrah POS";
        private string _currentUserName = string.Empty;
        private string _currentUserRole = string.Empty;
        private bool _isAdmin = false;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public string CurrentUserRole
        {
            get => _currentUserRole;
            set => SetProperty(ref _currentUserRole, value);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            // Initialize current user information
            var session = SessionService.Instance;
            if (session.IsLoggedIn && session.CurrentUser != null)
            {
                CurrentUserName = session.CurrentUser.Username;
                CurrentUserRole = session.CurrentUser.Role == UserRoles.Admin ? "مدير" : "كاشير";
                IsAdmin = session.IsAdmin;
            }

            LogoutCommand = new RelayCommand(_ => Logout());
        }

        private void Logout()
        {
            var result = MessageBox.Show(
                "هل تريد تسجيل الخروج؟",
                "تأكيد الخروج",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear session
                SessionService.Instance.Logout();

                // Open login window
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();

                // Close main window
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.MainWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            }
        }
    }
}
