using System.Windows;
using System.Windows.Media;
using JamrahPOS.Models;
using JamrahPOS.Services;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for OrderDetailsDialog.xaml
    /// </summary>
    public partial class OrderDetailsDialog : Window
    {
        private readonly Order _order;
        private readonly PrintService _printService;

        public OrderDetailsDialog(Order order)
        {
            InitializeComponent();
            
            _order = order;
            _printService = new PrintService();

            // Set order information
            OrderNumberText.Text = $"رقم الطلب: {order.OrderNumber}";
            DateTimeText.Text = order.OrderDateTime.ToString("yyyy/MM/dd - HH:mm:ss");
            CashierText.Text = order.Cashier?.Username ?? "غير معروف";
            PaymentMethodText.Text = order.PaymentMethod;
            TotalAmountText.Text = $"{order.TotalAmount:N2} جنيه";

            // Set status
            if (order.IsVoided)
            {
                StatusText.Text = "ملغي";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                StatusText.Text = "نشط";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }

            // Set order items
            ItemsListControl.ItemsSource = order.OrderItems;
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Generate receipt text
                var receiptText = _printService.GenerateReceipt(_order, _order.Cashier);
                
                // Print the receipt
                await _printService.PrintReceiptAsync(receiptText);
                
                MessageBox.Show("تم طباعة الفاتورة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في طباعة الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
