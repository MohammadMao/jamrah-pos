using System.Windows;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for PaymentMethodDialog.xaml
    /// </summary>
    public partial class PaymentMethodDialog : Window
    {
        public string PaymentMethod { get; private set; } = "نقداً";
        public bool ShouldPrint { get; private set; } = true;

        public PaymentMethodDialog(decimal totalAmount)
        {
            InitializeComponent();
            TotalAmountText.Text = $"{totalAmount:N2} ريال";
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (CashRadioButton.IsChecked == true)
            {
                PaymentMethod = "نقداً";
            }
            else if (CardRadioButton.IsChecked == true)
            {
                PaymentMethod = "بطاقة";
            }
            else if (OnlineRadioButton.IsChecked == true)
            {
                PaymentMethod = "تحويل بنكي";
            }

            ShouldPrint = PrintReceiptCheckBox.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
