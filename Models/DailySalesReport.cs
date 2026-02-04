using System.Globalization;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents daily sales report data
    /// </summary>
    public class DailySalesReport
    {
        private static readonly string[] ArabicDayNames = new[] 
        { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };
        
        public DateTime Date { get; set; }
        public string FormattedDate => $"{Date:yyyy-MM-dd} {ArabicDayNames[(int)Date.DayOfWeek]}";
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        public List<PaymentMethodSummary> PaymentMethods { get; set; } = new();
        public List<CashierSummary> Cashiers { get; set; } = new();
        public decimal AverageOrderValue => OrderCount > 0 ? TotalSales / OrderCount : 0;
    }
}
