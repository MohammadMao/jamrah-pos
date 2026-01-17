namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents an item in the shopping cart (temporary order)
    /// </summary>
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
