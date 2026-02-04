using System.Windows;
using System.Windows.Controls;
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
        private UserControl? _currentView;

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

        public UserControl? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ICommand LogoutCommand { get; }
        public ICommand NavigateToPosCommand { get; }
        public ICommand NavigateToOrdersCommand { get; }
        public ICommand NavigateToCategoriesCommand { get; }
        public ICommand NavigateToMenuItemsCommand { get; }
        public ICommand NavigateToUsersCommand { get; }
        public ICommand NavigateToInventoryCommand { get; }
        public ICommand NavigateToReportsCommand { get; }

        public MainViewModel()
        {
            try
            {
                Console.WriteLine("[MAIN] MainViewModel constructor started");
                // Initialize current user information
                var session = SessionService.Instance;
                if (session.IsLoggedIn && session.CurrentUser != null)
                {
                    CurrentUserName = session.CurrentUser.Username;
                    CurrentUserRole = session.CurrentUser.Role == UserRoles.Admin ? "مدير" : "كاشير";
                    IsAdmin = session.IsAdmin;
                    Console.WriteLine($"[MAIN] User session loaded - Username: {CurrentUserName}, Role: {CurrentUserRole}, IsAdmin: {IsAdmin}");
                }
                else
                {
                    Console.WriteLine("[MAIN] WARNING: No user session found");
                }

                LogoutCommand = new RelayCommand(_ => Logout());
                NavigateToPosCommand = new RelayCommand(_ => NavigateToPos());
                NavigateToOrdersCommand = new RelayCommand(_ => NavigateToOrders());
                NavigateToCategoriesCommand = new RelayCommand(_ => NavigateToCategories());
                NavigateToMenuItemsCommand = new RelayCommand(_ => NavigateToMenuItems());
                NavigateToUsersCommand = new RelayCommand(_ => NavigateToUsers());
                NavigateToInventoryCommand = new RelayCommand(_ => NavigateToInventory());
                NavigateToReportsCommand = new RelayCommand(_ => NavigateToReports());

                // Start with POS view
                NavigateToPos();
                Console.WriteLine("[MAIN] MainViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] CRITICAL ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[MAIN] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[MAIN] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        private void NavigateToPos()
        {
            try
            {
                Console.WriteLine("[MAIN] Navigating to POS view");
                CurrentView = new Views.PosView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] ERROR navigating to POS: {ex.Message}");
                MessageBox.Show($"خطأ في فتح شاشة نقطة البيع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToOrders()
        {
            try
            {
                Console.WriteLine("[MAIN] Navigating to Orders view");
                CurrentView = new Views.OrdersView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] ERROR navigating to Orders: {ex.Message}");
                MessageBox.Show($"خطأ في فتح شاشة الطلبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToCategories()
        {
            try
            {
                Console.WriteLine("[MAIN] Navigating to Categories view");
                CurrentView = new Views.CategoriesView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] ERROR navigating to Categories: {ex.Message}");
                Console.WriteLine($"[MAIN] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[MAIN] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في فتح شاشة التصنيفات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToMenuItems()
        {
            try
            {
                Console.WriteLine("[MAIN] Navigating to MenuItems view");
                CurrentView = new Views.MenuItemsView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] ERROR navigating to MenuItems: {ex.Message}");
                MessageBox.Show($"خطأ في فتح شاشة قائمة الطعام: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToUsers()
        {
            try
            {
                Console.WriteLine("[MAIN] Navigating to Users view");
                CurrentView = new Views.UsersView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] ERROR navigating to Users: {ex.Message}");
                Console.WriteLine($"[MAIN] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[MAIN] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في فتح شاشة المستخدمين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToInventory()
        {
            try
            {
                Console.WriteLine("[MAIN] Navigating to Inventory view");
                CurrentView = new Views.InventoryView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] ERROR navigating to Inventory: {ex.Message}");
                Console.WriteLine($"[MAIN] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[MAIN] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في فتح شاشة المخزون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToReports()
        {
            try
            {
                Console.WriteLine("[MAIN] Navigating to Reports view");
                CurrentView = new Views.ReportsView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] ERROR navigating to Reports: {ex.Message}");
                Console.WriteLine($"[MAIN] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[MAIN] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في فتح شاشة التقارير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Logout()
        {
            try
            {
                Console.WriteLine("[MAIN] Logout requested");
                var result = MessageBox.Show(
                    "هل تريد تسجيل الخروج؟",
                    "تأكيد الخروج",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Console.WriteLine("[MAIN] User confirmed logout");
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
                    Console.WriteLine("[MAIN] Logout completed");
                }
                else
                {
                    Console.WriteLine("[MAIN] Logout cancelled");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] ERROR during logout: {ex.Message}");
                MessageBox.Show($"خطأ في تسجيل الخروج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
