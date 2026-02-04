using System;
using System.ComponentModel.DataAnnotations;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents an inventory item
    /// </summary>
    public class InventoryItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty; // kg, bags, pieces, etc.

        public decimal Quantity { get; set; }

        public decimal MinimumQuantity { get; set; } // Alert threshold

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastModified { get; set; }
    }
}
