using System.Windows;
using System.Windows.Input;
using JamrahPOS.Data;
using JamrahPOS.Helpers;
using JamrahPOS.Services;

namespace JamrahPOS.ViewModels
{
    /// <summary>
    /// ViewModel for the Login window
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private AuthenticationService? _authService;  // Nullable
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading = false;

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ErrorMessage = string.Empty;
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ErrorMessage = string.Empty;
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand ExitCommand { get; }

        public LoginViewModel()
        {
            Console.WriteLine("[LOGIN] LoginViewModel constructor called");
            try
            {
                var context = new PosDbContext();
                _authService = new AuthenticationService(context);
                Console.WriteLine("[LOGIN] AuthenticationService initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[LOGIN] Stack trace: {ex.StackTrace}");
                ErrorMessage = $"خطأ في التهيئة: {ex.Message}";
            }

            // Use delegate instead of async lambda to properly execute async method
            LoginCommand = new RelayCommand(_ => 
            {
                Console.WriteLine("[LOGIN] Login button clicked");
                Console.WriteLine($"[LOGIN] Username: {Username}, IsLoading: {IsLoading}");
                _ = LoginAsync(); // Fire and forget with proper error handling
            }, _ => !IsLoading);
            
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            Console.WriteLine("[LOGIN] Commands initialized");
        }

        private async Task LoginAsync()
        {
            Console.WriteLine("[LOGIN] LoginAsync started");
            
            if (string.IsNullOrWhiteSpace(Username))
            {
                Console.WriteLine("[LOGIN] Username is empty");
                ErrorMessage = "الرجاء إدخال اسم المستخدم"; // Please enter username
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                Console.WriteLine("[LOGIN] Password is empty");
                ErrorMessage = "الرجاء إدخال كلمة المرور"; // Please enter password
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            Console.WriteLine($"[LOGIN] Attempting authentication for user: {Username}");

            try
            {
                var user = await _authService.AuthenticateAsync(Username, Password);
                Console.WriteLine($"[LOGIN] Authentication result: {(user != null ? "Success" : "Failed")}");

                if (user != null)
                {
                    Console.WriteLine($"[LOGIN] User authenticated: {user.Username}, Role: {user.Role}");
                    
                    // Store user in session
                    SessionService.Instance.Login(user);
                    Console.WriteLine("[LOGIN] User stored in session");

                    // Close login window and open main window
                    var mainWindow = new Views.MainWindow();
                    mainWindow.Show();
                    Console.WriteLine("[LOGIN] Main window shown");

                    // Close login window
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is Views.LoginWindow)
                        {
                            window.Close();
                            Console.WriteLine("[LOGIN] Login window closed");
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[LOGIN] Authentication failed - invalid credentials");
                    ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة"; // Invalid username or password
                    MessageBox.Show("اسم المستخدم أو كلمة المرور غير صحيحة", "خطأ في تسجيل الدخول", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] EXCEPTION during authentication: {ex.Message}");
                Console.WriteLine($"[LOGIN] Stack trace: {ex.StackTrace}");
                ErrorMessage = $"خطأ في تسجيل الدخول: {ex.Message}"; // Login error
            }
            finally
            {
                IsLoading = false;
                Console.WriteLine("[LOGIN] LoginAsync finished");
            }
        }
    }
}
