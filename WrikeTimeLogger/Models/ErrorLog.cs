using System.ComponentModel.DataAnnotations;

namespace WrikeTimeLogger.Models
{
    public class ErrorLog
    {
        public int Id { get; set; }
        public string? WrikeId { get; set; }
        //public string? StackTrace { get; set; }
        public string? Template { get; set; }
        [MaxLength(150)]
        public string? SourceContext { get; set; }
        public LogLevel LogLevel { get; set; }
        public string? Exception { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
