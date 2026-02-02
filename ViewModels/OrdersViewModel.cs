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
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalOrders = 0;
        private const int PageSize = 10;

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
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public User? SelectedCashier
        {
            get => _selectedCashier;
            set => SetProperty(ref _selectedCashier, value);
        }

        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        public bool ShowVoided
        {
            get => _showVoided;
            set => SetProperty(ref _showVoided, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalOrders
        {
            get => _totalOrders;
            set => SetProperty(ref _totalOrders, value);
        }

        public bool IsAdmin => SessionService.Instance.IsAdmin;

        public ICommand ViewDetailsCommand { get; }
        public ICommand VoidOrderCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand GoToPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

        public OrdersViewModel()
        {
            try
            {
                Console.WriteLine("[ORDERS] OrdersViewModel constructor started");
                _context = new PosDbContext();

                // Set default date range to today
                _startDate = DateTime.Today;
                _endDate = DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59);

                ViewDetailsCommand = new RelayCommand(param => ViewOrderDetails(param as Order));
                VoidOrderCommand = new RelayCommand(async param => await VoidOrderAsync(param as Order), _ => IsAdmin);
                RefreshCommand = new RelayCommand(async _ => await LoadOrdersAsync());
                SearchCommand = new RelayCommand(async _ => await SearchOrdersAsync());
                ResetFiltersCommand = new RelayCommand(async _ => await ResetFiltersAsync());
                GoToPageCommand = new RelayCommand(async param => await GoToPageAsync((int)param));
                PreviousPageCommand = new RelayCommand(async _ => await GoToPageAsync(CurrentPage - 1), _ => CurrentPage > 1);
                NextPageCommand = new RelayCommand(async _ => await GoToPageAsync(CurrentPage + 1), _ => CurrentPage < TotalPages);

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

                // Don't select any cashier by default (null = all cashiers)
                SelectedCashier = null;

                // Load initial orders (last 10)
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

        private async Task SearchOrdersAsync()
        {
            CurrentPage = 1; // Reset to first page
            await LoadOrdersAsync();
        }

        private async Task ResetFiltersAsync()
        {
            // Reset all filters to default
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59);
            SelectedCashier = null;
            SelectedPaymentMethod = "الكل";
            ShowVoided = true;
            CurrentPage = 1;
            
            // Load latest orders
            await LoadOrdersAsync();
        }

        private async Task GoToPageAsync(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > TotalPages)
                return;

            CurrentPage = pageNumber;
            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            IsLoading = true;
            try
            {
                Console.WriteLine($"[ORDERS] LoadOrdersAsync - Page {CurrentPage}");
                Console.WriteLine($"[ORDERS] StartDate: {StartDate:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"[ORDERS] EndDate: {EndDate:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"[ORDERS] SelectedCashier: {SelectedCashier?.Username ?? "null"}");
                Console.WriteLine($"[ORDERS] SelectedPaymentMethod: {SelectedPaymentMethod}");
                Console.WriteLine($"[ORDERS] ShowVoided: {ShowVoided}");
                
                // First, let's see what payment methods exist in the database
                var allPaymentMethods = await _context.Orders
                    .Select(o => o.PaymentMethod)
                    .Distinct()
                    .ToListAsync();
                Console.WriteLine($"[ORDERS] Payment methods in database: {string.Join(", ", allPaymentMethods)}");
                
                var query = _context.Orders
                    .Include(o => o.Cashier)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .AsQueryable();

                // Filter by date range - add time to end date to include full day
                var endDateWithTime = EndDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                Console.WriteLine($"[ORDERS] Adjusted EndDate: {endDateWithTime:yyyy-MM-dd HH:mm:ss}");
                query = query.Where(o => o.OrderDateTime >= StartDate && o.OrderDateTime <= endDateWithTime);

                // Filter by cashier
                if (SelectedCashier != null)
                {
                    Console.WriteLine($"[ORDERS] Filtering by cashier ID: {SelectedCashier.Id}");
                    query = query.Where(o => o.CashierId == SelectedCashier.Id);
                }

                // Filter by payment method
                if (SelectedPaymentMethod != "الكل")
                {
                    Console.WriteLine($"[ORDERS] Filtering by payment method: {SelectedPaymentMethod}");
                    query = query.Where(o => o.PaymentMethod == SelectedPaymentMethod);
                }

                // Filter voided orders
                if (!ShowVoided)
                {
                    Console.WriteLine($"[ORDERS] Filtering out voided orders");
                    query = query.Where(o => !o.IsVoided);
                }

                // Get total count for pagination
                TotalOrders = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalOrders / (double)PageSize);

                Console.WriteLine($"[ORDERS] Total Orders: {TotalOrders}, Total Pages: {TotalPages}");

                // Apply pagination
                var orders = await query
                    .OrderByDescending(o => o.OrderDateTime)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                Console.WriteLine($"[ORDERS] Loaded {orders.Count} orders for page {CurrentPage}");

                Orders = new ObservableCollection<Order>(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ORDERS] ERROR in LoadOrdersAsync: {ex.Message}");
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
