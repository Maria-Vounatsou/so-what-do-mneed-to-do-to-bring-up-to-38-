namespace WrikeTimeLogger.Models
{
    public class ClientSettings
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string RedirectUri { get; set; }
        public required string TokenUrl { get; set; }
        public required string BaseUrl { get; set; }
        public required string WebhookUrl { get; set; }
        public required string EmailHost { get; set; }
    }
}
