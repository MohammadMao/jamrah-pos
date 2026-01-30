using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using JamrahPOS.Data;
using JamrahPOS.Helpers;
using JamrahPOS.Models;
using JamrahPOS.Services;
using Microsoft.EntityFrameworkCore;

namespace JamrahPOS.ViewModels
{
    /// <summary>
    /// ViewModel for the Orders Management screen
    /// </summary>
    public class OrdersViewModel : BaseViewModel
    {
        private readonly PosDbContext _context;
        private ObservableCollection<Order> _orders = new();
        private ObservableCollection<User> _cashiers = new();
        private DateTime _startDate;
        private DateTime _endDate;
        private User? _selectedCashier;
        private string _selectedPaymentMethod = "الكل";
        private bool _showVoided = true;
        private bool _isLoading;

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public ObservableCollection<User> Cashiers
        {
            get => _cashiers;
            set => SetProperty(ref _cashiers, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = LoadOrdersAsync();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _ = LoadOrdersAsync();
                }
            }
        }

        public User? SelectedCashier
        {
            get => _selectedCashier;
            set
            {
                if (SetProperty(ref _selectedCashier, value))
                {
                    _ = LoadOrdersAsync();
                }
            }
        }

        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (SetProperty(ref _selectedPaymentMethod, value))
                {
                    _ = LoadOrdersAsync();
                }
            }
        }

        public bool ShowVoided
        {
            get => _showVoided;
            set
            {
                if (SetProperty(ref _showVoided, value))
                {
                    _ = LoadOrdersAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsAdmin => SessionService.Instance.IsAdmin;

        public ICommand ViewDetailsCommand { get; }
        public ICommand VoidOrderCommand { get; }
        public ICommand RefreshCommand { get; }

        public OrdersViewModel()
        {
            try
            {
                Console.WriteLine("[ORDERS] OrdersViewModel constructor started");
                _context = new PosDbContext();

                // Set default date range to today
                _startDate = DateTime.Today;
                _endDate = DateTime.Today.AddDays(1).AddSeconds(-1);

                ViewDetailsCommand = new RelayCommand(param => ViewOrderDetails(param as Order));
                VoidOrderCommand = new RelayCommand(async param => await VoidOrderAsync(param as Order), _ => IsAdmin);
                RefreshCommand = new RelayCommand(async _ => await LoadOrdersAsync());

                _ = LoadDataAsync();
                Console.WriteLine("[ORDERS] OrdersViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ORDERS] CRITICAL ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[ORDERS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[ORDERS] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                Console.WriteLine("[ORDERS] LoadDataAsync started");
                // Load cashiers for filter
                var cashiers = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Username)
                    .ToListAsync();
                Console.WriteLine($"[ORDERS] Loaded {cashiers.Count} cashiers");
                Cashiers = new ObservableCollection<User>(cashiers);

                await LoadOrdersAsync();
                Console.WriteLine("[ORDERS] LoadDataAsync completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ORDERS] ERROR in LoadDataAsync: {ex.Message}");
                Console.WriteLine($"[ORDERS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[ORDERS] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadOrdersAsync()
        {
            IsLoading = true;
            try
            {
                var query = _context.Orders
                    .Include(o => o.Cashier)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .AsQueryable();

                // Filter by date range
                query = query.Where(o => o.OrderDateTime >= StartDate && o.OrderDateTime <= EndDate);

                // Filter by cashier
                if (SelectedCashier != null)
                {
                    query = query.Where(o => o.CashierId == SelectedCashier.Id);
                }

                // Filter by payment method
                if (SelectedPaymentMethod != "الكل")
                {
                    query = query.Where(o => o.PaymentMethod == SelectedPaymentMethod);
                }

                // Filter voided orders
                if (!ShowVoided)
                {
                    query = query.Where(o => !o.IsVoided);
                }

                var orders = await query
                    .OrderByDescending(o => o.OrderDateTime)
                    .ToListAsync();

                Orders = new ObservableCollection<Order>(orders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الطلبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ViewOrderDetails(Order? order)
        {
            if (order == null) return;

            var dialog = new Views.OrderDetailsDialog(order);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        private async Task VoidOrderAsync(Order? order)
        {
            if (order == null) return;

            if (order.IsVoided)
            {
                MessageBox.Show("هذا الطلب ملغي بالفعل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل أنت متأكد من إلغاء الطلب {order.OrderNumber}؟\nلا يمكن التراجع عن هذا الإجراء.",
                "تأكيد الإلغاء",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                order.IsVoided = true;
                await _context.SaveChangesAsync();

                MessageBox.Show("تم إلغاء الطلب بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إلغاء الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
