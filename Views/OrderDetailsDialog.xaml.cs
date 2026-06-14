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
            TotalAmountText.Text = $"{order.TotalAmount:N2} SDG";

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
                bool ok = await _printService.PrintKitchenOrderAsync(_order);
                if (ok)
                    MessageBox.Show("تم طباعة الفاتورة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("فشلت الطباعة. تأكد من إعدادات الطابعة.", "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في طباعة الفاتورة:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
