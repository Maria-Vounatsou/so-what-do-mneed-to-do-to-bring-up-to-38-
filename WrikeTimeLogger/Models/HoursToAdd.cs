using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WrikeTimeLogger.Models
{
    public class HoursToAdd
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Task")]
        public string TaskId { get; set; }  // Foreign key
        public string? WrikeId { get; set; }
        public double Hours { get; set; }
        public DateOnly DateIn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public DateTime? DateUp { get; set; }
    }
}
