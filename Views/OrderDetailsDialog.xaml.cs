using System.Windows;
using System.Windows.Media;
using JamrahPOS.Models;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for OrderDetailsDialog.xaml
    /// </summary>
    public partial class OrderDetailsDialog : Window
    {
        public OrderDetailsDialog(Order order)
        {
            InitializeComponent();

            // Set order information
            OrderNumberText.Text = $"رقم الطلب: {order.OrderNumber}";
            DateTimeText.Text = order.OrderDateTime.ToString("yyyy/MM/dd - HH:mm:ss");
            CashierText.Text = order.Cashier?.Username ?? "غير معروف";
            PaymentMethodText.Text = order.PaymentMethod;
            TotalAmountText.Text = $"{order.TotalAmount:N2} ريال";

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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
