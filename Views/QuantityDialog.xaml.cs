using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for QuantityDialog.xaml
    /// </summary>
    public partial class QuantityDialog : Window
    {
        public int Quantity { get; private set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public QuantityDialog(string itemName, decimal price)
        {
            InitializeComponent();
            
            ItemName = itemName;
            Price = price;
            Quantity = 1;

            ItemNameText.Text = itemName;
            PriceText.Text = $"{price:N2} ريال";
            QuantityTextBox.Text = "1";
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

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityTextBox.Text, out int quantity) && quantity > 0)
            {
                Quantity = quantity;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("الرجاء إدخال كمية صحيحة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow numbers
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            Regex regex = new Regex("[^0-9]+");
            return !regex.IsMatch(text);
        }
    }
}
