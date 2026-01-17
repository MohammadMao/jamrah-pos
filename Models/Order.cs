using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents a customer order
    /// </summary>
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public DateTime OrderDateTime { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, etc.

        [Required]
        public int CashierId { get; set; }

        public bool IsVoided { get; set; } = false;

        // Navigation properties
        [ForeignKey(nameof(CashierId))]
        public virtual User Cashier { get; set; } = null!;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
