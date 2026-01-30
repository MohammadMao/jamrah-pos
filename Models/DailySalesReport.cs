namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents daily sales report data
    /// </summary>
    public class DailySalesReport
    {
        public DateTime Date { get; set; }
        public string FormattedDate => Date.ToString("yyyy-MM-dd dddd");
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        public List<PaymentMethodSummary> PaymentMethods { get; set; } = new();
        public List<CashierSummary> Cashiers { get; set; } = new();
        public decimal AverageOrderValue => OrderCount > 0 ? TotalSales / OrderCount : 0;
    }
}
