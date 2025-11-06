namespace WrikeTimeLogger.Models
{
    public class TimeTracker
    {
        public int id { get; set; }
        public string userId { get; set; }
        public string taskId { get; set; }
        public double hours { get; set; }
        public DateOnly date { get; set; }/* = DateOnly.FromDateTime(DateTime.UtcNow);*/
        public string timeTrackerId { get; set; }
    }
}
