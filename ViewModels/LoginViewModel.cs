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
        private readonly AuthenticationService _authService;
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
            var context = new PosDbContext();
            _authService = new AuthenticationService(context);

            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoading);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "الرجاء إدخال اسم المستخدم"; // Please enter username
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "الرجاء إدخال كلمة المرور"; // Please enter password
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = await _authService.AuthenticateAsync(Username, Password);

                if (user != null)
                {
                    // Store user in session
                    SessionService.Instance.Login(user);

                    // Close login window and open main window
                    var mainWindow = new Views.MainWindow();
                    mainWindow.Show();

                    // Close login window
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is Views.LoginWindow)
                        {
                            window.Close();
                            break;
                        }
                    }
                }
                else
                {
                    ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة"; // Invalid username or password
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ في تسجيل الدخول: {ex.Message}"; // Login error
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
