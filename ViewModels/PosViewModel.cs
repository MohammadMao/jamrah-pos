using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using JamrahPOS.Data;
using JamrahPOS.Helpers;
using JamrahPOS.Models;
using JamrahPOS.Services;

namespace JamrahPOS.ViewModels
{
    /// <summary>
    /// ViewModel for the POS/Cashier screen
    /// </summary>
    public class PosViewModel : BaseViewModel
    {
        private readonly OrderService _orderService;
        private readonly PrintService _printService;
        private ObservableCollection<Category> _categories = new();
        private ObservableCollection<MenuItem> _menuItems = new();
        private ObservableCollection<CartItem> _cartItems = new();
        private Category? _selectedCategory;
        private decimal _totalAmount;
        private bool _isLoading;

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<MenuItem> MenuItems
        {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
        }

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    _ = LoadMenuItemsAsync();
                }
            }
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand ShowAllCategoriesCommand { get; }

        public PosViewModel()
        {
            try
            {
                Console.WriteLine("[POS] PosViewModel constructor started");
                var context = new PosDbContext();
                _orderService = new OrderService(context);
                _printService = new PrintService();

                // Subscribe to cart items collection changes
                _orderService.CartItems.CollectionChanged += (s, e) => UpdateCart();

                AddItemCommand = new RelayCommand(param => AddItem(param as MenuItem));
                RemoveItemCommand = new RelayCommand(param => RemoveItem(param as CartItem));
                EditItemCommand = new RelayCommand(param => EditItem(param as CartItem));
                ClearCartCommand = new RelayCommand(_ => ClearCart());
                CheckoutCommand = new RelayCommand(async _ => await CheckoutAsync(), _ => CartItems.Count > 0);
                ShowAllCategoriesCommand = new RelayCommand(_ => ShowAllCategories());

                _ = LoadDataAsync();
                Console.WriteLine("[POS] PosViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[POS] CRITICAL ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[POS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[POS] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                Console.WriteLine("[POS] LoadDataAsync started");
                var categories = await _orderService.GetCategoriesAsync();
                Console.WriteLine($"[POS] Loaded {categories.Count} categories");
                Categories = new ObservableCollection<Category>(categories);

                await LoadMenuItemsAsync();
                Console.WriteLine("[POS] LoadDataAsync completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[POS] ERROR in LoadDataAsync: {ex.Message}");
                Console.WriteLine($"[POS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[POS] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMenuItemsAsync()
        {
            IsLoading = true;
            try
            {
                Console.WriteLine($"[POS] LoadMenuItemsAsync - Category: {SelectedCategory?.Name ?? "All"}");
                var items = await _orderService.GetMenuItemsByCategoryAsync(SelectedCategory?.Id);
                Console.WriteLine($"[POS] Loaded {items.Count} menu items");
                MenuItems = new ObservableCollection<MenuItem>(items);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[POS] ERROR in LoadMenuItemsAsync: {ex.Message}");
                Console.WriteLine($"[POS] Stack trace: {ex.StackTrace}");
                MessageBox.Show($"خطأ في تحميل الأصناف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddItem(MenuItem? menuItem)
        {
            if (menuItem == null) return;

            // Show quantity dialog
            var dialog = new Views.QuantityDialog(menuItem.Name, menuItem.Price);
            
            if (dialog.ShowDialog() == true)
            {
                _orderService.AddToCart(menuItem, dialog.Quantity);
                UpdateCart();
            }
        }

        private void RemoveItem(CartItem? item)
        {
            if (item == null) return;

            var result = MessageBox.Show(
                $"هل تريد حذف {item.Name} من الطلب؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _orderService.RemoveFromCart(item);
                UpdateCart();
            }
        }

        private void EditItem(CartItem? item)
        {
            if (item == null) return;

            // Show edit dialog
            var dialog = new Views.EditItemDialog(item);

            if (dialog.ShowDialog() == true)
            {
                _orderService.UpdateCartItem(item, dialog.Quantity, dialog.UnitPrice);
                UpdateCart();
            }
        }

        private void ClearCart()
        {
            if (CartItems.Count == 0) return;

            var result = MessageBox.Show(
                "هل تريد مسح جميع الأصناف من الطلب؟",
                "تأكيد المسح",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _orderService.ClearCart();
                UpdateCart();
            }
        }

        private async Task CheckoutAsync()
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("الطلب فارغ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var currentUser = SessionService.Instance.CurrentUser;
            if (currentUser == null)
            {
                MessageBox.Show("لم يتم تسجيل الدخول", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Show payment method dialog
            var paymentDialog = new Views.PaymentMethodDialog(TotalAmount);

            if (paymentDialog.ShowDialog() != true)
            {
                return; // User cancelled
            }

            try
            {
                IsLoading = true;
                var order = await _orderService.SaveOrderAsync(currentUser.Id, paymentDialog.PaymentMethod);

                // Generate receipt
                var receiptText = _printService.GenerateReceipt(order, currentUser);

                // Save receipt to file
                var receiptPath = await _printService.SaveReceiptToFileAsync(receiptText, order.OrderNumber);

                // Print if requested
                if (paymentDialog.ShouldPrint)
                {
                    var printSuccess = await _printService.PrintReceiptAsync(receiptText);
                    
                    if (!printSuccess)
                    {
                        // If printing fails, ask if user wants to open the file
                        var openResult = MessageBox.Show(
                            "فشلت الطباعة. هل تريد فتح الفاتورة؟",
                            "خطأ في الطباعة",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (openResult == MessageBoxResult.Yes)
                        {
                            _printService.OpenReceipt(receiptPath);
                        }
                    }
                }

                MessageBox.Show(
                    $"تم حفظ الطلب بنجاح\nرقم الطلب: {order.OrderNumber}\nالإجمالي: {order.TotalAmount:N2} جنيه",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                UpdateCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ShowAllCategories()
        {
            SelectedCategory = null;
        }

        private void UpdateCart()
        {
            CartItems = new ObservableCollection<CartItem>(_orderService.CartItems);
            TotalAmount = _orderService.TotalAmount;
            OnPropertyChanged(nameof(CartItems));
            OnPropertyChanged(nameof(TotalAmount));
        }
    }
}
