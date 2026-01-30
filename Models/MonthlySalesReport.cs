namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents monthly sales report data
    /// </summary>
    public class MonthlySalesReport
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string FormattedPeriod => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        public List<DailySalesReport> DailyBreakdown { get; set; } = new();
        public List<PaymentMethodSummary> PaymentMethods { get; set; } = new();
        public List<CashierSummary> Cashiers { get; set; } = new();
        public decimal AverageOrderValue => OrderCount > 0 ? TotalSales / OrderCount : 0;
    }
}
