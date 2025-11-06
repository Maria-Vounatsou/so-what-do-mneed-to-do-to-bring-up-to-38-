namespace WrikeTimeLogger.Models
{
    public class Counters
    {
        public int Id { get; set; }
        public DateOnly DateIn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public string WrikeId { get; set; }


    
    }
    
}
