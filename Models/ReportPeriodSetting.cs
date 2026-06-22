using System.ComponentModel.DataAnnotations;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents a custom report time period setting for daily report filtering.
    /// </summary>
    public class ReportPeriodSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The selected start hour for the period (0-23).
        /// </summary>
        public int StartHour { get; set; }

        /// <summary>
        /// The selected end hour for display (0-23).
        /// </summary>
        public int EndHour { get; set; }

        /// <summary>
        /// The effective end time in minutes after midnight.
        /// If the admin selected 00:00, this will be stored as 23:59 (1439).
        /// </summary>
        public int EndTimeMinutes { get; set; }
    }
}
