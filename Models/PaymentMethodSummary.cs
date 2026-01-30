namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents a summary of sales grouped by payment method
    /// </summary>
    public class PaymentMethodSummary
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int OrderCount { get; set; }
        public decimal Percentage { get; set; }
    }
}
