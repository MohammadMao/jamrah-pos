namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents a summary of sales grouped by cashier
    /// </summary>
    public class CashierSummary
    {
        public int CashierId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int OrderCount { get; set; }
        public decimal Percentage { get; set; }
    }
}
