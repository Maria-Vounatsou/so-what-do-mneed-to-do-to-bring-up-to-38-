using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WrikeTimeLogger.Models
{
    public class User
    {
        [Key, JsonIgnore]
        public string WrikeId { get; set; }
        [JsonIgnore]
        public string Name { get; set; }
        [JsonPropertyName("access_token"), JsonInclude]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token"), JsonInclude]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in"), JsonInclude]
        public int ExpiresIn { get; set; }
        [JsonIgnore]
        public string Role { get; set; }
        [JsonIgnore]
        public bool IsEnabled { get; set; } = true;
        [JsonPropertyName("email"), JsonInclude]
        public string Email { get; set; }
    }
}
