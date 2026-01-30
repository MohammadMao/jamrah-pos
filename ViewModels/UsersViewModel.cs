using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using JamrahPOS.Data;
using JamrahPOS.Helpers;
using JamrahPOS.Models;
using JamrahPOS.Services;
using Microsoft.EntityFrameworkCore;

namespace JamrahPOS.ViewModels
{
    /// <summary>
    /// ViewModel for Users Management screen (Admin only)
    /// </summary>
    public class UsersViewModel : BaseViewModel
    {
        private readonly PosDbContext _context;
        private readonly AuthenticationService _authService;
        private ObservableCollection<User> _users = new();
        private bool _isLoading;

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand RefreshCommand { get; }

        public UsersViewModel()
        {
            try
            {
                Console.WriteLine("[USERS] UsersViewModel constructor started");
                _context = new PosDbContext();
                _authService = new AuthenticationService(_context);

                AddUserCommand = new RelayCommand(_ => AddUser());
                EditUserCommand = new RelayCommand(param => EditUser(param as User));
                ToggleActiveCommand = new RelayCommand(async param => await ToggleActiveAsync(param as User));
                ResetPasswordCommand = new RelayCommand(async param => await ResetPasswordAsync(param as User));
                RefreshCommand = new RelayCommand(async _ => await LoadUsersAsync());

                Console.WriteLine("[USERS] Commands initialized, loading users...");
                _ = LoadUsersAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[USERS] CRITICAL ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[USERS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[USERS] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        private async Task LoadUsersAsync()
        {
            IsLoading = true;
            try
            {
                Console.WriteLine("[USERS] Loading users...");
                var users = await _context.Users
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                Console.WriteLine($"[USERS] Retrieved {users.Count} users from database");
                Users = new ObservableCollection<User>(users);
                Console.WriteLine("[USERS] Users loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[USERS] ERROR loading users: {ex.Message}");
                Console.WriteLine($"[USERS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[USERS] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في تحميل المستخدمين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddUser()
        {
            var dialog = new Views.UserDialog();
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                _ = SaveUserAsync(null, dialog.Username, dialog.Password, dialog.Role, dialog.IsActiveStatus);
            }
        }

        private void EditUser(User? user)
        {
            if (user == null) return;

            var dialog = new Views.UserDialog(user);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                _ = SaveUserAsync(user, dialog.Username, dialog.Password, dialog.Role, dialog.IsActiveStatus, dialog.IsPasswordChanged);
            }
        }

        private async Task SaveUserAsync(User? user, string username, string password, string role, bool isActive, bool isPasswordChanged = true)
        {
            try
            {
                IsLoading = true;

                if (user == null)
                {
                    // Check if username already exists
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == username);

                    if (existingUser != null)
                    {
                        MessageBox.Show("اسم المستخدم موجود بالفعل", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Add new user
                    var newUser = new User
                    {
                        Username = username,
                        PasswordHash = AuthenticationService.HashPassword(password),
                        Role = role,
                        IsActive = isActive
                    };

                    _context.Users.Add(newUser);
                }
                else
                {
                    // Update existing user
                    user.Role = role;
                    user.IsActive = isActive;

                    // Update password if changed
                    if (isPasswordChanged && !string.IsNullOrWhiteSpace(password))
                    {
                        user.PasswordHash = AuthenticationService.HashPassword(password);
                    }
                }

                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ المستخدم بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleActiveAsync(User? user)
        {
            if (user == null) return;

            // Prevent deactivating self
            var currentUser = SessionService.Instance.CurrentUser;
            if (currentUser != null && user.Id == currentUser.Id)
            {
                MessageBox.Show("لا يمكنك تعطيل حسابك الخاص", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var action = user.IsActive ? "تعطيل" : "تفعيل";
            var result = MessageBox.Show(
                $"هل تريد {action} المستخدم '{user.Username}'؟",
                $"تأكيد {action}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                MessageBox.Show($"تم {action} المستخدم بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في {action} المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ResetPasswordAsync(User? user)
        {
            if (user == null) return;

            var result = MessageBox.Show(
                $"هل تريد إعادة تعيين كلمة المرور للمستخدم '{user.Username}'؟\nكلمة المرور الجديدة ستكون: password123",
                "تأكيد إعادة تعيين كلمة المرور",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                user.PasswordHash = AuthenticationService.HashPassword("password123");
                await _context.SaveChangesAsync();

                MessageBox.Show(
                    $"تم إعادة تعيين كلمة المرور بنجاح\nكلمة المرور الجديدة: password123",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إعادة تعيين كلمة المرور: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
