namespace JamrahPOS.Models
{
    /// <summary>
    /// Aggregated sales data for a single menu item within a report period
    /// </summary>
    public class MenuItemSalesReport
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
