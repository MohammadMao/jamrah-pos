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
        public ICommand ClearCartCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand ShowAllCategoriesCommand { get; }

        public PosViewModel()
        {
            var context = new PosDbContext();
            _orderService = new OrderService(context);

            // Subscribe to cart items collection changes
            _orderService.CartItems.CollectionChanged += (s, e) => UpdateCart();

            AddItemCommand = new RelayCommand(async param => await AddItemAsync(param as MenuItem));
            RemoveItemCommand = new RelayCommand(param => RemoveItem(param as CartItem));
            ClearCartCommand = new RelayCommand(_ => ClearCart());
            CheckoutCommand = new RelayCommand(async _ => await CheckoutAsync(), _ => CartItems.Count > 0);
            ShowAllCategoriesCommand = new RelayCommand(_ => ShowAllCategories());

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var categories = await _orderService.GetCategoriesAsync();
                Categories = new ObservableCollection<Category>(categories);

                await LoadMenuItemsAsync();
            }
            catch (Exception ex)
            {
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
                var items = await _orderService.GetMenuItemsByCategoryAsync(SelectedCategory?.Id);
                MenuItems = new ObservableCollection<MenuItem>(items);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الأصناف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddItemAsync(MenuItem? menuItem)
        {
            if (menuItem == null) return;

            // Show quantity dialog
            var dialog = new Views.QuantityDialog(menuItem.Name, menuItem.Price);
            dialog.Owner = Application.Current.MainWindow;
            
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

            // TODO: Show payment dialog to select payment method
            // For now, default to Cash
            var paymentMethod = "نقداً";

            try
            {
                IsLoading = true;
                var order = await _orderService.SaveOrderAsync(currentUser.Id, paymentMethod);

                MessageBox.Show(
                    $"تم حفظ الطلب بنجاح\nرقم الطلب: {order.OrderNumber}\nالإجمالي: {order.TotalAmount:N2} ريال",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                UpdateCart();
                await Task.CompletedTask;
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
