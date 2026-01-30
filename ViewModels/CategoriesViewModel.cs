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
    /// ViewModel for Categories Management screen
    /// </summary>
    public class CategoriesViewModel : BaseViewModel
    {
        private readonly PosDbContext _context;
        private ObservableCollection<Category> _categories = new();
        private bool _isLoading;

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand AddCategoryCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand RefreshCommand { get; }

        public CategoriesViewModel()
        {
            try
            {
                Console.WriteLine("[CATEGORIES] CategoriesViewModel constructor started");
                _context = new PosDbContext();

                AddCategoryCommand = new RelayCommand(_ => AddCategory());
                EditCategoryCommand = new RelayCommand(param => EditCategory(param as Category));
                ToggleActiveCommand = new RelayCommand(async param => await ToggleActiveAsync(param as Category));
                RefreshCommand = new RelayCommand(async _ => await LoadCategoriesAsync());

                Console.WriteLine("[CATEGORIES] Commands initialized, loading categories...");
                _ = LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CATEGORIES] CRITICAL ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[CATEGORIES] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[CATEGORIES] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            IsLoading = true;
            try
            {
                Console.WriteLine("[CATEGORIES] Loading categories...");
                var categories = await _context.Categories
                    .Include(c => c.MenuItems)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                Console.WriteLine($"[CATEGORIES] Loaded {categories.Count} categories");
                Categories = new ObservableCollection<Category>(categories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CATEGORIES] ERROR loading categories: {ex.Message}");
                Console.WriteLine($"[CATEGORIES] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[CATEGORIES] Inner exception: {ex.InnerException?.Message}");
                MessageBox.Show($"خطأ في تحميل التصنيفات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddCategory()
        {
            var dialog = new Views.CategoryDialog();
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                _ = SaveCategoryAsync(null, dialog.CategoryName, dialog.IsActiveStatus);
            }
        }

        private void EditCategory(Category? category)
        {
            if (category == null) return;

            var dialog = new Views.CategoryDialog(category);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                _ = SaveCategoryAsync(category, dialog.CategoryName, dialog.IsActiveStatus);
            }
        }

        private async Task SaveCategoryAsync(Category? category, string name, bool isActive)
        {
            try
            {
                IsLoading = true;

                if (category == null)
                {
                    // Add new category
                    var newCategory = new Category
                    {
                        Name = name,
                        IsActive = isActive
                    };

                    _context.Categories.Add(newCategory);
                }
                else
                {
                    // Update existing category
                    category.Name = name;
                    category.IsActive = isActive;
                }

                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ التصنيف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ التصنيف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleActiveAsync(Category? category)
        {
            if (category == null) return;

            var action = category.IsActive ? "تعطيل" : "تفعيل";
            var result = MessageBox.Show(
                $"هل تريد {action} التصنيف '{category.Name}'؟",
                $"تأكيد {action}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                category.IsActive = !category.IsActive;
                await _context.SaveChangesAsync();

                MessageBox.Show($"تم {action} التصنيف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في {action} التصنيف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
