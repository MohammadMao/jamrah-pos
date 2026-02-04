using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JamrahPOS.Data;
using JamrahPOS.Helpers;
using JamrahPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace JamrahPOS.ViewModels
{
    /// <summary>
    /// ViewModel for Inventory Management screen
    /// </summary>
    public class InventoryViewModel : BaseViewModel
    {
        private readonly PosDbContext _context;
        private ObservableCollection<InventoryItem> _inventoryItems = new();
        private bool _isLoading;
        private string _searchText = string.Empty;

        public ObservableCollection<InventoryItem> InventoryItems
        {
            get => _inventoryItems;
            set => SetProperty(ref _inventoryItems, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadInventoryItemsAsync();
                }
            }
        }

        public ICommand AddItemCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand IncrementQuantityCommand { get; }
        public ICommand DecrementQuantityCommand { get; }
        public ICommand RefreshCommand { get; }

        public InventoryViewModel()
        {
            _context = new PosDbContext();

            AddItemCommand = new RelayCommand(_ => AddItem());
            EditItemCommand = new RelayCommand(param => EditItem(param as InventoryItem));
            DeleteItemCommand = new RelayCommand(param => DeleteItem(param as InventoryItem));
            IncrementQuantityCommand = new RelayCommand(param => AdjustQuantity(param as InventoryItem, 1));
            DecrementQuantityCommand = new RelayCommand(param => AdjustQuantity(param as InventoryItem, -1));
            RefreshCommand = new RelayCommand(_ => _ = LoadInventoryItemsAsync());

            _ = LoadInventoryItemsAsync();
        }

        private async Task LoadInventoryItemsAsync()
        {
            try
            {
                IsLoading = true;

                var query = _context.InventoryItems.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(i => i.Name.Contains(SearchText));
                }

                var items = await query.OrderBy(i => i.Name).ToListAsync();
                InventoryItems = new ObservableCollection<InventoryItem>(items);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المخزون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddItem()
        {
            var dialog = new Views.InventoryItemDialog();
            if (dialog.ShowDialog() == true && dialog.Item != null)
            {
                try
                {
                    _context.InventoryItems.Add(dialog.Item);
                    _context.SaveChanges();
                    _ = LoadInventoryItemsAsync();
                    MessageBox.Show("تم إضافة الصنف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في الإضافة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditItem(InventoryItem? item)
        {
            if (item == null) return;

            var dialog = new Views.InventoryItemDialog(item);
            if (dialog.ShowDialog() == true && dialog.Item != null)
            {
                try
                {
                    var existingItem = _context.InventoryItems.Find(item.Id);
                    if (existingItem != null)
                    {
                        existingItem.Name = dialog.Item.Name;
                        existingItem.Unit = dialog.Item.Unit;
                        existingItem.Quantity = dialog.Item.Quantity;
                        existingItem.MinimumQuantity = dialog.Item.MinimumQuantity;
                        existingItem.Notes = dialog.Item.Notes;
                        existingItem.LastModified = DateTime.Now;

                        _context.SaveChanges();
                        _ = LoadInventoryItemsAsync();
                        MessageBox.Show("تم تعديل الصنف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteItem(InventoryItem? item)
        {
            if (item == null) return;

            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف '{item.Name}'؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.InventoryItems.Remove(item);
                    _context.SaveChanges();
                    _ = LoadInventoryItemsAsync();
                    MessageBox.Show("تم حذف الصنف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void AdjustQuantity(InventoryItem? item, decimal adjustment)
        {
            if (item == null) return;

            try
            {
                var existingItem = await _context.InventoryItems.FindAsync(item.Id);
                if (existingItem != null)
                {
                    existingItem.Quantity += adjustment;
                    if (existingItem.Quantity < 0) existingItem.Quantity = 0;
                    existingItem.LastModified = DateTime.Now;

                    await _context.SaveChangesAsync();
                    await LoadInventoryItemsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تعديل الكمية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
