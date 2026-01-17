using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using JamrahPOS.Data;
using JamrahPOS.Helpers;
using JamrahPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace JamrahPOS.ViewModels
{
    /// <summary>
    /// ViewModel for Menu Items Management screen
    /// </summary>
    public class MenuItemsViewModel : BaseViewModel
    {
        private readonly PosDbContext _context;
        private ObservableCollection<MenuItem> _menuItems = new();
        private ObservableCollection<Category> _categories = new();
        private Category? _selectedCategory;
        private bool _isLoading;

        public ObservableCollection<MenuItem> MenuItems
        {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
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

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand AddMenuItemCommand { get; }
        public ICommand EditMenuItemCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand DeleteMenuItemCommand { get; }
        public ICommand RefreshCommand { get; }

        public MenuItemsViewModel()
        {
            _context = new PosDbContext();

            AddMenuItemCommand = new RelayCommand(_ => AddMenuItem());
            EditMenuItemCommand = new RelayCommand(param => EditMenuItem(param as MenuItem));
            ToggleActiveCommand = new RelayCommand(async param => await ToggleActiveAsync(param as MenuItem));
            DeleteMenuItemCommand = new RelayCommand(async param => await DeleteMenuItemAsync(param as MenuItem));
            RefreshCommand = new RelayCommand(async _ => await LoadMenuItemsAsync());

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();
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
                var query = _context.MenuItems
                    .Include(m => m.Category)
                    .AsQueryable();

                if (SelectedCategory != null)
                {
                    query = query.Where(m => m.CategoryId == SelectedCategory.Id);
                }

                var menuItems = await query
                    .OrderBy(m => m.Name)
                    .ToListAsync();

                MenuItems = new ObservableCollection<MenuItem>(menuItems);
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

        private void AddMenuItem()
        {
            var categories = Categories.ToList();
            if (!categories.Any())
            {
                MessageBox.Show("الرجاء إضافة تصنيف واحد على الأقل أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Views.MenuItemDialog(categories);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                _ = SaveMenuItemAsync(null, dialog.ItemName, dialog.Price, dialog.CategoryId, dialog.IsActiveStatus);
            }
        }

        private void EditMenuItem(MenuItem? menuItem)
        {
            if (menuItem == null) return;

            var categories = Categories.ToList();
            var dialog = new Views.MenuItemDialog(categories, menuItem);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                _ = SaveMenuItemAsync(menuItem, dialog.ItemName, dialog.Price, dialog.CategoryId, dialog.IsActiveStatus);
            }
        }

        private async Task SaveMenuItemAsync(MenuItem? menuItem, string name, decimal price, int categoryId, bool isActive)
        {
            try
            {
                IsLoading = true;

                if (menuItem == null)
                {
                    // Add new menu item
                    var newMenuItem = new MenuItem
                    {
                        Name = name,
                        Price = price,
                        CategoryId = categoryId,
                        IsActive = isActive
                    };

                    _context.MenuItems.Add(newMenuItem);
                }
                else
                {
                    // Update existing menu item
                    menuItem.Name = name;
                    menuItem.Price = price;
                    menuItem.CategoryId = categoryId;
                    menuItem.IsActive = isActive;
                }

                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ الصنف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadMenuItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الصنف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleActiveAsync(MenuItem? menuItem)
        {
            if (menuItem == null) return;

            var action = menuItem.IsActive ? "تعطيل" : "تفعيل";
            var result = MessageBox.Show(
                $"هل تريد {action} الصنف '{menuItem.Name}'؟",
                $"تأكيد {action}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                menuItem.IsActive = !menuItem.IsActive;
                await _context.SaveChangesAsync();

                MessageBox.Show($"تم {action} الصنف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadMenuItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في {action} الصنف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteMenuItemAsync(MenuItem? menuItem)
        {
            if (menuItem == null) return;

            // Check if item has been used in orders
            var hasOrders = await _context.OrderItems
                .AnyAsync(oi => oi.MenuItemId == menuItem.Id);

            if (hasOrders)
            {
                MessageBox.Show(
                    "لا يمكن حذف هذا الصنف لأنه تم استخدامه في طلبات سابقة.\nيمكنك تعطيله بدلاً من ذلك.",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف الصنف '{menuItem.Name}'؟\nلا يمكن التراجع عن هذا الإجراء.",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم حذف الصنف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadMenuItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف الصنف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
