using System.Collections.ObjectModel;
using System.Linq;
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
            try
            {
                Console.WriteLine("[USERS] AddUser called");
                Console.WriteLine("[USERS] Creating UserDialog...");
                var dialog = new Views.UserDialog();
                dialog.Topmost = true;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Console.WriteLine("[USERS] Dialog created, showing...");
                var result = dialog.ShowDialog();
                Console.WriteLine($"[USERS] Dialog closed with result: {result}");
                
                if (result == true)
                {
                    Console.WriteLine($"[USERS] Saving user: {dialog.Username}, Role: {dialog.Role}");
                    _ = SaveUserAsync(null, dialog.Username, dialog.Password, dialog.Role, dialog.IsActiveStatus);
                }
                else
                {
                    Console.WriteLine("[USERS] Dialog was cancelled");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[USERS] ERROR in AddUser: {ex.Message}");
                Console.WriteLine($"[USERS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[USERS] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في فتح نافذة الإضافة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditUser(User? user)
        {
            if (user == null) return;

            try
            {
                Console.WriteLine($"[USERS] EditUser called for user: {user.Username}");
                Console.WriteLine("[USERS] Creating UserDialog...");
                var dialog = new Views.UserDialog(user);
                dialog.Topmost = true;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Console.WriteLine("[USERS] Dialog created, showing...");
                var result = dialog.ShowDialog();
                Console.WriteLine($"[USERS] Dialog closed with result: {result}");
                
                if (result == true)
                {
                    Console.WriteLine($"[USERS] Saving user changes: {dialog.Username}, Role: {dialog.Role}, PasswordChanged: {dialog.IsPasswordChanged}");
                    _ = SaveUserAsync(user, dialog.Username, dialog.Password, dialog.Role, dialog.IsActiveStatus, dialog.IsPasswordChanged);
                }
                else
                {
                    Console.WriteLine("[USERS] Dialog was cancelled");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[USERS] ERROR in EditUser: {ex.Message}");
                Console.WriteLine($"[USERS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[USERS] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في فتح نافذة التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveUserAsync(User? user, string username, string password, string role, bool isActive, bool isPasswordChanged = true)
        {
            try
            {
                Console.WriteLine($"[USERS] SaveUserAsync started - Username: {username}, Role: {role}, IsActive: {isActive}");
                IsLoading = true;

                if (user == null)
                {
                    Console.WriteLine("[USERS] Adding new user");
                    // Check if username already exists
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == username);

                    if (existingUser != null)
                    {
                        Console.WriteLine("[USERS] Username already exists");
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

                    Console.WriteLine($"[USERS] New user created with hash: {newUser.PasswordHash.Substring(0, 10)}...");
                    _context.Users.Add(newUser);
                }
                else
                {
                    Console.WriteLine($"[USERS] Updating existing user: {user.Username}");
                    
                    // Re-fetch the user from database to ensure it's tracked
                    var dbUser = await _context.Users.FindAsync(user.Id);
                    if (dbUser == null)
                    {
                        Console.WriteLine("[USERS] ERROR: User not found in database");
                        MessageBox.Show("لم يتم العثور على المستخدم في قاعدة البيانات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // Update existing user
                    dbUser.Role = role;
                    dbUser.IsActive = isActive;

                    // Update password if changed
                    if (isPasswordChanged && !string.IsNullOrWhiteSpace(password))
                    {
                        Console.WriteLine($"[USERS] Updating password, new password length: {password.Length}");
                        var newHash = AuthenticationService.HashPassword(password);
                        Console.WriteLine($"[USERS] Old hash: {dbUser.PasswordHash.Substring(0, 20)}...");
                        Console.WriteLine($"[USERS] New hash: {newHash.Substring(0, 20)}...");
                        dbUser.PasswordHash = newHash;
                    }
                    else
                    {
                        Console.WriteLine($"[USERS] Password not changed (isPasswordChanged: {isPasswordChanged}, password length: {password?.Length ?? 0})");
                    }
                }

                Console.WriteLine("[USERS] Saving changes to database...");
                await _context.SaveChangesAsync();
                Console.WriteLine("[USERS] Changes saved successfully");

                MessageBox.Show("تم حفظ المستخدم بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[USERS] ERROR in SaveUserAsync: {ex.Message}");
                Console.WriteLine($"[USERS] Stack trace: {ex.StackTrace}");
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
    }
}
