using System.ComponentModel.DataAnnotations;

namespace WrikeTimeLogger.Models
{
    public class UsersTasks
    {
        public string Workflow { get; set; }
        public string UserId { get; set; }
        public string TaskId { get; set; }
        public string Status { get; set; }
        public DateTime DateIn { get; set; } = DateTime.UtcNow;
        public DateTime? DateUpt { get; set; }
        public bool IsAutomated { get; set; } = false;


        public User User { get; set; }
        public ICollection<HoursToAdd> HoursToAdd { get; set; } // Reference to HoursToAdd
    }
}
