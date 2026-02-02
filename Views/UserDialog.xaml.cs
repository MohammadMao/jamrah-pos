using System.Windows;
using System.Windows.Controls;
using JamrahPOS.Helpers;
using JamrahPOS.Models;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for UserDialog.xaml
    /// </summary>
    public partial class UserDialog : Window
    {
        private readonly User? _user;
        private string _password = string.Empty;

        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public string Role { get; private set; } = UserRoles.Cashier;
        public bool IsActiveStatus { get; private set; } = true;
        public bool IsPasswordChanged { get; private set; } = false;

        public UserDialog(User? user = null)
        {
            Console.WriteLine("[UserDialog] Constructor called");
            InitializeComponent();
            Console.WriteLine("[UserDialog] InitializeComponent completed");

            _user = user;

            if (_user != null)
            {
                Console.WriteLine($"[UserDialog] Edit mode for user: {_user.Username}");
                // Edit mode
                TitleText.Text = "تعديل المستخدم";
                UsernameTextBox.Text = _user.Username;
                UsernameTextBox.IsEnabled = false; // Username cannot be changed
                
                // Set role
                foreach (ComboBoxItem item in RoleComboBox.Items)
                {
                    if (item.Tag.ToString() == _user.Role)
                    {
                        RoleComboBox.SelectedItem = item;
                        break;
                    }
                }

                IsActiveCheckBox.IsChecked = _user.IsActive;

                // Show password hint for edit mode
                PasswordLabelText.Text = "كلمة المرور الجديدة";
                PasswordHintText.Visibility = Visibility.Visible;
            }
            else
            {
                Console.WriteLine("[UserDialog] Add mode");
            }

            UsernameTextBox.Focus();
            Console.WriteLine("[UserDialog] Constructor completed");
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _password = PasswordBox.Password;
            Console.WriteLine($"[UserDialog] PasswordBox changed, length: {_password.Length}");
            // Sync with TextBox if it's visible
            if (PasswordTextBox.Visibility == Visibility.Visible)
            {
                PasswordTextBox.Text = _password;
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _password = PasswordTextBox.Text;
            Console.WriteLine($"[UserDialog] PasswordTextBox changed, length: {_password.Length}");
            // Sync with PasswordBox if it's visible
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                PasswordBox.Password = _password;
            }
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                // Show password as text
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                TogglePasswordButton.Content = "👁‍🗨"; // Eye with slash icon
            }
            else
            {
                // Hide password
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                TogglePasswordButton.Content = "👁"; // Eye icon
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[UserDialog] Save button clicked");
            
            try
            {
                // Validate username
                Console.WriteLine("[UserDialog] Validating username...");
                var username = UsernameTextBox.Text.Trim();
                Console.WriteLine($"[UserDialog] Username: '{username}'");
                
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("[UserDialog] Username validation failed - empty");
                    ShowError("الرجاء إدخال اسم المستخدم");
                    MessageBox.Show("الرجاء إدخال اسم المستخدم", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate password (required for new users only)
                Console.WriteLine($"[UserDialog] Validating password... (IsNewUser: {_user == null}, Password length: {_password.Length})");
                if (_user == null && string.IsNullOrWhiteSpace(_password))
                {
                    Console.WriteLine("[UserDialog] Password validation failed - required for new user");
                    ShowError("الرجاء إدخال كلمة المرور");
                    MessageBox.Show("الرجاء إدخال كلمة المرور", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate password length if provided
                if (!string.IsNullOrWhiteSpace(_password) && _password.Length < 6)
                {
                    Console.WriteLine("[UserDialog] Password validation failed - too short");
                    ShowError("يجب أن تكون كلمة المرور 6 أحرف على الأقل");
                    MessageBox.Show("يجب أن تكون كلمة المرور 6 أحرف على الأقل", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get role
                Console.WriteLine("[UserDialog] Getting role...");
                if (RoleComboBox.SelectedItem is not ComboBoxItem selectedItem)
                {
                    Console.WriteLine("[UserDialog] Role validation failed - no selection");
                    ShowError("الرجاء اختيار دور وظيفي");
                    MessageBox.Show("الرجاء اختيار دور وظيفي", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Console.WriteLine($"[UserDialog] Role: {selectedItem.Tag}");
                
                Username = username;
                Password = _password;
                Role = selectedItem.Tag.ToString() ?? UserRoles.Cashier;
                IsActiveStatus = IsActiveCheckBox.IsChecked == true;
                IsPasswordChanged = !string.IsNullOrWhiteSpace(_password);

                Console.WriteLine($"[UserDialog] All validation passed. IsPasswordChanged: {IsPasswordChanged}, Password length: {Password.Length}");
                Console.WriteLine("[UserDialog] Setting DialogResult=true and closing...");
                DialogResult = true;
                Close();
                Console.WriteLine("[UserDialog] Close() called");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserDialog] ERROR in Save_Click: {ex.Message}");
                Console.WriteLine($"[UserDialog] Stack trace: {ex.StackTrace}");
                ShowError($"خطأ: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[UserDialog] Cancel button clicked");
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
