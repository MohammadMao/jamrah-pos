using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JamrahPOS.Models;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for MenuItemDialog.xaml
    /// </summary>
    public partial class MenuItemDialog : Window
    {
        private readonly Models.MenuItem? _menuItem;

        public string ItemName { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int CategoryId { get; private set; }
        public bool IsActiveStatus { get; private set; } = true;

        public MenuItemDialog(List<Category> categories, Models.MenuItem? menuItem = null)
        {
            InitializeComponent();

            _menuItem = menuItem;

            // Load categories
            CategoryComboBox.ItemsSource = categories.Where(c => c.IsActive).ToList();

            if (_menuItem != null)
            {
                // Edit mode
                TitleText.Text = "تعديل الصنف";
                NameTextBox.Text = _menuItem.Name;
                PriceTextBox.Text = _menuItem.Price.ToString("F2");
                CategoryComboBox.SelectedItem = categories.FirstOrDefault(c => c.Id == _menuItem.CategoryId);
                IsActiveCheckBox.IsChecked = _menuItem.IsActive;
            }
            else if (categories.Any())
            {
                // Default to first category for new items
                CategoryComboBox.SelectedIndex = 0;
            }

            NameTextBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate name
            var name = NameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("الرجاء إدخال اسم الصنف");
                return;
            }

            // Validate price
            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                ShowError("الرجاء إدخال سعر صحيح");
                return;
            }

            // Validate category
            if (CategoryComboBox.SelectedItem is not Category selectedCategory)
            {
                ShowError("الرجاء اختيار تصنيف");
                return;
            }

            ItemName = name;
            Price = price;
            CategoryId = selectedCategory.Id;
            IsActiveStatus = IsActiveCheckBox.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !IsTextDecimal(newText);
        }

        private static bool IsTextDecimal(string text)
        {
            Regex regex = new Regex("^[0-9]*\\.?[0-9]*$");
            return regex.IsMatch(text);
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
