using System.Windows;
using JamrahPOS.Models;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for CategoryDialog.xaml
    /// </summary>
    public partial class CategoryDialog : Window
    {
        private readonly Category? _category;

        public string CategoryName { get; private set; } = string.Empty;
        public bool IsActiveStatus { get; private set; } = true;

        public CategoryDialog(Category? category = null)
        {
            InitializeComponent();

            _category = category;

            if (_category != null)
            {
                // Edit mode
                TitleText.Text = "تعديل التصنيف";
                NameTextBox.Text = _category.Name;
                IsActiveCheckBox.IsChecked = _category.IsActive;
            }

            NameTextBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate name
            var name = NameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("الرجاء إدخال اسم التصنيف");
                return;
            }

            CategoryName = name;
            IsActiveStatus = IsActiveCheckBox.IsChecked == true;

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
