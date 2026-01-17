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
            InitializeComponent();

            _user = user;

            if (_user != null)
            {
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

            UsernameTextBox.Focus();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _password = PasswordBox.Password;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate username
            var username = UsernameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("الرجاء إدخال اسم المستخدم");
                return;
            }

            // Validate password (required for new users only)
            if (_user == null && string.IsNullOrWhiteSpace(_password))
            {
                ShowError("الرجاء إدخال كلمة المرور");
                return;
            }

            // Validate password length if provided
            if (!string.IsNullOrWhiteSpace(_password) && _password.Length < 6)
            {
                ShowError("يجب أن تكون كلمة المرور 6 أحرف على الأقل");
                return;
            }

            // Get role
            if (RoleComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                ShowError("الرجاء اختيار دور وظيفي");
                return;
            }

            Username = username;
            Password = _password;
            Role = selectedItem.Tag.ToString() ?? UserRoles.Cashier;
            IsActiveStatus = IsActiveCheckBox.IsChecked == true;
            IsPasswordChanged = !string.IsNullOrWhiteSpace(_password);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
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
