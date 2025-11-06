namespace WrikeTimeLogger.Models
{
    public class ContactsResponse
    {
        public string kind { get; set; }
        public Datum[] data { get; set; }
    }

    public class Datum
    {
        public string id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string type { get; set; }
        public Profile[] profiles { get; set; }
        public string avatarUrl { get; set; }
        public string timezone { get; set; }
        public string locale { get; set; }
        public bool deleted { get; set; }
        public bool me { get; set; }
        public string title { get; set; }
        public string primaryEmail { get; set; }
    }

    public class Profile
    {
        public string accountId { get; set; }
        public string email { get; set; }
        public string role { get; set; }
        public bool external { get; set; }
        public bool admin { get; set; }
        public bool owner { get; set; }
    }    
}
