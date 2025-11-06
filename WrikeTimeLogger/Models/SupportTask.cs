using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WrikeTimeLogger.Models
{
    [Table("SupportTasks")]
    public class SupportTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TaskId { get; set; } = null!;

        public string? Link { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = "";

        [Required]
        [MaxLength(1000)]
        public string TeamName { get; set; } = "";

        public bool Completed { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? AssignedToUserId { get; set; }

        public bool ReassignedAway { get; set; } = false;
    }
}
