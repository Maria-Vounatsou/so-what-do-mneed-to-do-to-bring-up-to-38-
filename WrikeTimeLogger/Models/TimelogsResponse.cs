namespace WrikeTimeLogger.Models
{
    public class TimelogsResponse
    {
        public string Kind { get; set; }
        public List<TimelogsDatum> Data { get; set; }


        public class TimelogsDatum
        {
            public string Id { get; set; }
            public string UserId { get; set; }
            public string TaskId { get; set; }
            public double Hours { get; set; }
            public string TrackedDate { get; set; }
            public DateTime CreatedDate { get; set; }
            public string UpdatedDate { get; set; }
            public string Comment { get; set; }

        }
    }
}
