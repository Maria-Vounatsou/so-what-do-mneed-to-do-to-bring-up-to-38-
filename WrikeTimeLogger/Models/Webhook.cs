namespace WrikeTimeLogger.Models;

public class Webhook
{
    public string kind { get; set; }
    public List<Datum> data { get; set; }

    public class Datum
    {
        public string id { get; set; }
        public string accountId { get; set; }
        public string hookUrl { get; set; }
        public string status { get; set; }
        public string[] events { get; set; }
    }
}


