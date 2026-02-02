using System.Collections.ObjectModel;
using JamrahPOS.Data;
using JamrahPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace JamrahPOS.Services
{
    /// <summary>
    /// Service for managing current order/cart operations
    /// </summary>
    public class OrderService
    {
        private readonly PosDbContext _context;
        private readonly ObservableCollection<CartItem> _cartItems;

        public ObservableCollection<CartItem> CartItems => _cartItems;
        public decimal TotalAmount => _cartItems.Sum(item => item.TotalPrice);

        public OrderService(PosDbContext context)
        {
            _context = context;
            _cartItems = new ObservableCollection<CartItem>();
        }

        /// <summary>
        /// Adds an item to the cart or increases quantity if it already exists
        /// </summary>
        public void AddToCart(MenuItem menuItem, int quantity)
        {
            var existingItem = _cartItems.FirstOrDefault(x => x.MenuItemId == menuItem.Id);

            if (existingItem != null)
            {
                // Item already in cart, increase quantity
                existingItem.Quantity += quantity;
            }
            else
            {
                // Add new item to cart
                _cartItems.Add(new CartItem
                {
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    UnitPrice = menuItem.Price,
                    Quantity = quantity
                });
            }
        }

        /// <summary>
        /// Removes an item from the cart
        /// </summary>
        public void RemoveFromCart(CartItem item)
        {
            _cartItems.Remove(item);
        }

        /// <summary>
        /// Updates the quantity of an item in the cart
        /// </summary>
        public void UpdateQuantity(CartItem item, int newQuantity)
        {
            if (newQuantity <= 0)
            {
                RemoveFromCart(item);
            }
            else
            {
                item.Quantity = newQuantity;
            }
        }

        /// <summary>
        /// Updates the price of an item in the cart (for applying discounts)
        /// </summary>
        public void UpdatePrice(CartItem item, decimal newPrice)
        {
            if (newPrice < 0)
            {
                throw new ArgumentException("Price cannot be negative", nameof(newPrice));
            }

            item.UnitPrice = newPrice;
        }

        /// <summary>
        /// Updates both quantity and price of an item in the cart
        /// </summary>
        public void UpdateCartItem(CartItem item, int newQuantity, decimal newPrice)
        {
            if (newQuantity <= 0)
            {
                RemoveFromCart(item);
            }
            else
            {
                item.Quantity = newQuantity;
                item.UnitPrice = newPrice;
            }
        }

        /// <summary>
        /// Clears all items from the cart
        /// </summary>
        public void ClearCart()
        {
            _cartItems.Clear();
        }

        /// <summary>
        /// Saves the current cart as an order
        /// </summary>
        public async Task<Order> SaveOrderAsync(int cashierId, string paymentMethod)
        {
            if (_cartItems.Count == 0)
            {
                throw new InvalidOperationException("Cannot save empty order");
            }

            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                OrderDateTime = DateTime.Now,
                TotalAmount = TotalAmount,
                PaymentMethod = paymentMethod,
                CashierId = cashierId,
                IsVoided = false
            };

            // Add order items
            foreach (var cartItem in _cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    MenuItemId = cartItem.MenuItemId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    TotalPrice = cartItem.TotalPrice
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Reload order with related data for printing
            var savedOrder = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Cashier)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            // Clear cart after successful save
            ClearCart();

            return savedOrder ?? order;
        }

        /// <summary>
        /// Generates a unique order number with daily incrementing counter
        /// </summary>
        private string GenerateOrderNumber()
        {
            var today = DateTime.Today;
            var todayString = today.ToString("yyyyMMdd");
            
            // Count orders created today
            var todayOrderCount = _context.Orders
                .Where(o => o.OrderDateTime >= today && o.OrderDateTime < today.AddDays(1))
                .Count();
            
            var dailyNumber = todayOrderCount + 1;
            return $"{todayString}-{dailyNumber}";
        }

        /// <summary>
        /// Gets all menu items by category
        /// </summary>
        public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(int? categoryId = null)
        {
            var query = _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive);

            if (categoryId.HasValue)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            return await query.OrderBy(m => m.Name).ToListAsync();
        }

        /// <summary>
        /// Gets all categories
        /// </summary>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }
    }
}
