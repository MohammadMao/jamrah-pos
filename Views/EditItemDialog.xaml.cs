using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using JamrahPOS.Models;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for EditItemDialog.xaml
    /// </summary>
    public partial class EditItemDialog : Window
    {
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        private readonly decimal _originalPrice;

        public EditItemDialog(CartItem item)
        {
            InitializeComponent();

            ItemNameText.Text = item.Name;
            _originalPrice = item.OriginalPrice;
            Quantity = item.Quantity;
            UnitPrice = item.UnitPrice;

            OriginalPriceText.Text = $"السعر الأصلي: {_originalPrice:N2} SDG";
            QuantityTextBox.Text = Quantity.ToString();
            PriceTextBox.Text = UnitPrice.ToString("F2");

            UpdateDiscountDisplay();
            QuantityTextBox.Focus();
            QuantityTextBox.SelectAll();
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityTextBox.Text, out int currentQuantity))
            {
                QuantityTextBox.Text = (currentQuantity + 1).ToString();
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityTextBox.Text, out int currentQuantity) && currentQuantity > 1)
            {
                QuantityTextBox.Text = (currentQuantity - 1).ToString();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("الرجاء إدخال كمية صحيحة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("الرجاء إدخال سعر صحيح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Quantity = quantity;
            UnitPrice = price;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !IsTextDecimal(newText);
        }

        private void PriceTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateDiscountDisplay();
        }

        private void UpdateDiscountDisplay()
        {
            if (decimal.TryParse(PriceTextBox.Text, out decimal currentPrice))
            {
                if (currentPrice < _originalPrice)
                {
                    decimal discount = _originalPrice - currentPrice;
                    decimal discountPercentage = (_originalPrice > 0) ? (discount / _originalPrice) * 100 : 0;
                    DiscountText.Text = $"خصم: {discount:N2} SDG ({discountPercentage:N1}%)";
                    DiscountText.Visibility = Visibility.Visible;
                }
                else
                {
                    DiscountText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                DiscountText.Visibility = Visibility.Collapsed;
            }
        }

        private static bool IsTextNumeric(string text)
        {
            Regex regex = new Regex("[^0-9]+");
            return !regex.IsMatch(text);
        }

        private static bool IsTextDecimal(string text)
        {
            Regex regex = new Regex("^[0-9]*\\.?[0-9]*$");
            return regex.IsMatch(text);
        }
    }
}
