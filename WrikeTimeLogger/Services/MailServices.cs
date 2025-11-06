using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WrikeTimeLogger.Models;

namespace WrikeTimeLogger.Services
{
    public class MailServices
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MailServices> _logger;
        private readonly ClientSettings _settings;

        public MailServices(HttpClient httpClient, IOptions<ClientSettings> settings, ILogger<MailServices> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<HttpResponseMessage> SendEmailAsync(string subject, string body, int priority, string recipients, string sendEmail)
        {
            try
            {
                var data = new
                {
                    Subject = subject,
                    Body = body,
                    Priority = priority,
                    Recipients = new string[] { recipients },
                    emailAccountId = 1
                };


                var result = await _httpClient.PostAsJsonAsync(_settings.EmailHost + "/api/Account/token", new { username = "WrikeTimeLogger", password = "Qwerty1234!" });
                result.EnsureSuccessStatusCode();
                var token = await result.Content.ReadAsStringAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsJsonAsync(_settings.EmailHost + "/api/Emails", data);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message,$"Error occured when trying to send mail to:{sendEmail}");
                return null;
            }
        }
    }
}
