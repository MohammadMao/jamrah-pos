using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents an item within an order
    /// </summary>
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey(nameof(MenuItemId))]
        public virtual MenuItem MenuItem { get; set; } = null!;
    }
}
