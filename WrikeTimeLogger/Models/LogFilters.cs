namespace WrikeTimeLogger.Models
{
    public sealed class LogFilters
    {
        public string? WrikeId { get; set; }
        public string? SourceContext { get; set; }
        public string? Exception { get; set; }
        public string? Template { get; set; }
        public string? LogEvent { get; set; }
        public int? LogLevel { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
