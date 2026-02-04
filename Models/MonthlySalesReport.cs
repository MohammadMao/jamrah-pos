using System.Globalization;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents monthly sales report data
    /// </summary>
    public class MonthlySalesReport
    {
        private static readonly string[] ArabicMonthNames = new[] 
        { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
        
        public int Year { get; set; }
        public int Month { get; set; }
        public string FormattedPeriod => $"{ArabicMonthNames[Month - 1]} {Year}";
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        public List<DailySalesReport> DailyBreakdown { get; set; } = new();
        public List<PaymentMethodSummary> PaymentMethods { get; set; } = new();
        public List<CashierSummary> Cashiers { get; set; } = new();
        public decimal AverageOrderValue => OrderCount > 0 ? TotalSales / OrderCount : 0;
    }
}
