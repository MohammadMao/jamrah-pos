using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using JamrahPOS.Models;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for InventoryItemDialog.xaml
    /// </summary>
    public partial class InventoryItemDialog : Window
    {
        public InventoryItem? Item { get; private set; }

        public InventoryItemDialog(InventoryItem? existingItem = null)
        {
            InitializeComponent();

            if (existingItem != null)
            {
                // Edit mode
                TitleText.Text = "تعديل صنف المخزون";
                NameTextBox.Text = existingItem.Name;
                UnitTextBox.Text = existingItem.Unit;
                QuantityTextBox.Text = existingItem.Quantity.ToString("F2");
                MinimumQuantityTextBox.Text = existingItem.MinimumQuantity.ToString("F2");
                NotesTextBox.Text = existingItem.Notes;
            }
            else
            {
                // Add mode - set defaults
                QuantityTextBox.Text = "0";
                MinimumQuantityTextBox.Text = "0";
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

            // Validate unit
            var unit = UnitTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(unit))
            {
                ShowError("الرجاء إدخال الوحدة");
                return;
            }

            // Validate quantity - default to 0 if empty
            decimal quantity = 0;
            if (!string.IsNullOrWhiteSpace(QuantityTextBox.Text))
            {
                if (!decimal.TryParse(QuantityTextBox.Text, out quantity) || quantity < 0)
                {
                    ShowError("الرجاء إدخال كمية صحيحة");
                    return;
                }
            }

            // Validate minimum quantity
            if (!decimal.TryParse(MinimumQuantityTextBox.Text, out decimal minQuantity) || minQuantity < 0)
            {
                ShowError("الرجاء إدخال حد أدنى صحيح");
                return;
            }

            Item = new InventoryItem
            {
                Name = name,
                Unit = unit,
                Quantity = quantity,
                MinimumQuantity = minQuantity,
                Notes = NotesTextBox.Text.Trim(),
                CreatedAt = DateTime.Now
            };

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
        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numbers and decimal point
            Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
            var textBox = sender as System.Windows.Controls.TextBox;
            var fullText = textBox?.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !regex.IsMatch(fullText ?? string.Empty);
        }
    }
}
