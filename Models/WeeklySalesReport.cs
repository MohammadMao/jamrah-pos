namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents weekly sales report data (Sunday to Saturday)
    /// </summary>
    public class WeeklySalesReport
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public string FormattedPeriod => $"{WeekStartDate:yyyy-MM-dd} to {WeekEndDate:yyyy-MM-dd}";
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        public List<DailySalesReport> DailyBreakdown { get; set; } = new();
        public List<PaymentMethodSummary> PaymentMethods { get; set; } = new();
        public List<CashierSummary> Cashiers { get; set; } = new();
        public decimal AverageOrderValue => OrderCount > 0 ? TotalSales / OrderCount : 0;
    }
}
