using System.ComponentModel.DataAnnotations;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents a user in the POS system (Admin or Cashier)
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Cashier"; // Admin or Cashier

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
