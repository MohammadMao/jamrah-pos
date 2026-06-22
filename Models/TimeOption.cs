using System.ComponentModel.DataAnnotations;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents an hour-of-day option shown in dropdowns.
    /// </summary>
    public class TimeOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Hour { get; set; }

        [Required]
        [MaxLength(10)]
        public string Label { get; set; } = string.Empty;
    }
}
