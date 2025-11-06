using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using WrikeTimeLogger.Data;
using WrikeTimeLogger.Models;
using System.Text.Json;



namespace WrikeTimeLogger.Services
{
    public sealed class WrikeService
    {
        private readonly HttpClient _client;
        private readonly IDbContextFactory<AppDbContext> _dbContext;
        private readonly ClientSettings _settings;
        private readonly ILogger<WrikeService> _logger;
        private readonly NavigationManager _navigation;
        private readonly MailServices _mailServices;

        private AuthenticationStateProvider _authenticationStateProvider { get; }

        public WrikeService(HttpClient client, IOptions<ClientSettings> settings, IDbContextFactory<AppDbContext> dbContext, AuthenticationStateProvider authenticationStateProvider, ILogger<WrikeService> logger, NavigationManager Navigation, MailServices mailServices)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _dbContext = dbContext;
            _settings = settings.Value;

            _authenticationStateProvider = authenticationStateProvider;
            _logger = logger;
            _navigation = Navigation;
            _mailServices = mailServices;
        }

        public static Dictionary<string, MyWorkFlows> WorkflowsIds { get; private set; } = [];

        public async Task LoadWorkFlows()
        {
            Dictionary<string, MyWorkFlows> workflowIds = new Dictionary<string, MyWorkFlows>();
            using var appDbContext = _dbContext.CreateDbContext();
            var user = await appDbContext.Users.Where(u => u.Role == "Admin").FirstOrDefaultAsync();
            var accessToken = await EnsureValidAccessTokenAsync(user!.AccessToken, user.WrikeId);
            if (accessToken == null)
            {
                user = await appDbContext.Users.Where(u => u.Role == "Admin" && u.Name != user.Name).FirstOrDefaultAsync();
                if (user == null)
                {
                    _logger.LogError("No Admin user found in the database, please add an Admin user to the database.");
                    return;
                }
                accessToken = await EnsureValidAccessTokenAsync(user.AccessToken, user.WrikeId);
            }
            var workflows = await GetWorkflowsAsync(accessToken);
            foreach (var workflow in workflows.data)
            {
                foreach (var customStatus in workflow.customStatuses)
                {
                    var myWorkFlows = new MyWorkFlows
                    {
                        workflowId = workflow.id,
                        workflowName = workflow.name,
                        customStatusName = customStatus.name
                    };
                    workflowIds[customStatus.id] = myWorkFlows;
                }
            }
            WorkflowsIds = workflowIds;
            if (WorkflowsIds.Count == 0)
            {
                _logger.LogError("No workflows found in the database, please add workflows to the database.");
            }
        }

        public string GetAuthorizationUrl()
        {
            return $"https://login.wrike.com/oauth2/authorize/v4?response_type=code&client_id={_settings.ClientId}&redirect_uri={_settings.RedirectUri}";
        }

        public async Task<User> GetTokensAsync(string code)
        {
            var requestData = new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = _settings.RedirectUri
            };
            try
            {
                var response = await _client.PostAsync(_settings.TokenUrl, new FormUrlEncodedContent(requestData));
                response.EnsureSuccessStatusCode();
                return (await response.Content.ReadFromJsonAsync<User>())!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting tokens for SignIn mapGet.");
                return null;
            }
        }

        internal async Task<string> RefreshAccessTokenAsync(string userId)
        {
            using var _appDbContext = _dbContext.CreateDbContext();
            HttpResponseMessage response = new HttpResponseMessage();
            var userToUpdate = await _appDbContext.Users.Where(u => u.WrikeId == userId).FirstOrDefaultAsync();
            if (userToUpdate != null)
            {
                try
                {
                    var requestBody = new Dictionary<string, string>
                    {
                        { "client_id", _settings.ClientId },
                        { "client_secret", _settings.ClientSecret },
                        { "grant_type", "refresh_token" },
                        { "refresh_token", userToUpdate.RefreshToken }
                    };

                    response = await _client.PostAsync(_settings.TokenUrl, new FormUrlEncodedContent(requestBody));
                    response.EnsureSuccessStatusCode();

                    var tokenResponse = await response.Content.ReadFromJsonAsync<User>();

                    if (tokenResponse != null)
                    {
                        userToUpdate.AccessToken = tokenResponse.AccessToken;
                        userToUpdate.RefreshToken = tokenResponse.RefreshToken;
                        userToUpdate.ExpiresIn = tokenResponse.ExpiresIn;

                        await _appDbContext.SaveChangesAsync();

                        return tokenResponse.AccessToken;
                    }
                    else
                    {
                        throw new Exception("Token response is null.");
                    }
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("400"))
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        //var mailResponse = await _mailServices.SendEmailAsync("WrikeTimeLogger Action required", "The refresh token for " + userToUpdate.Name + " has been expired and a relog is required", 2, userToUpdate.Email, "mail_send");
                        _logger.LogError(ex, $"HttpRequestError while refreshing access token for user {userToUpdate.Name}, refresh token is expired, a relog is required.");
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error while refreshing access token for user {userToUpdate.Name}.");
                    return null;
                }
            }
            else
            {
                _logger.LogInformation($"Cannot find {userId} on Db for refresh Token");
                return null;
            }
        }

        public async Task<string?> EnsureValidAccessTokenAsync(string accessToken, string userId)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.GetAsync($"{_settings.BaseUrl}/contacts?me");
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return await RefreshAccessTokenAsync(userId);
                }
                return accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ensuring valid access token.");
                return null;
            }
        }

        public async Task<ContactsResponse> GetWrikeIdAsync(string accessToken)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.GetAsync($"{_settings.BaseUrl}/contacts?me");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var contacts = System.Text.Json.JsonSerializer.Deserialize<ContactsResponse>(responseBody);
                return contacts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting WrikeId from GetWrikeIdAsync, returned null value.");
                return null;
            }
        }

        public async Task<TaskResponse?> GetActiveTasksAsync(string accessToken, string wrikeId)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var baseUrl = ($"{_settings.BaseUrl}/tasks?status=Active&responsibles=[{wrikeId}]&limit=1000");

                var allTasks = new List<WrikeTask>();
                string? nextPage = null;

                do
                {
                    var url = nextPage is null ? baseUrl: $"{baseUrl}&nextPageToken={Uri.EscapeDataString(nextPage)}";

                    var response = await _client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();

                    var pageResponse = JsonSerializer.Deserialize<TaskResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (pageResponse?.data != null)
                    {
                        var filtered = pageResponse.data
                            .Where(t => !(t.title?.Contains("New support request", StringComparison.OrdinalIgnoreCase) ?? false));
                        allTasks.AddRange(filtered);
                    }

                    using var doc = JsonDocument.Parse(json);
                    nextPage = doc.RootElement.TryGetProperty("nextPageToken", out var tokenEl) ? tokenEl.GetString() : null;
                }
                while (!string.IsNullOrEmpty(nextPage));

                return new TaskResponse { data = allTasks.ToArray() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting active tasks.");
                return null;
            }
        }

        public async Task<Workflows> GetWorkflowsAsync(string accessToken)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.GetAsync($"{_settings.BaseUrl}/workflows");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                // var contacts = System.Text.Json.JsonSerializer.Deserialize<Workflows>(responseBody);
                return await response.Content.ReadFromJsonAsync<Workflows>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting workflows.");
                return null;
            }
        }



        //public async Task<TaskResponse> GetActiveTasksAsyncWithWrikeId(string accessToken, string wrikeId)
        //{
        //    try
        //    {
        //        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //        var response = await _client.GetAsync($"{_settings.BaseUrl}/tasks?status=Active&responsibles=[{wrikeId}]&sortField=LastAccessDate");
        //        response.EnsureSuccessStatusCode();
        //        return await response.Content.ReadFromJsonAsync<TaskResponse>();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error while getting active tasks with WrikeId.");
        //        return null;
        //    }
        //}

        //public async Task<TimelogsResponse> GetTodaysTimeLogsAsync(string accessToken)
        //{
        //    try
        //    {
        //        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //        var response = await _client.GetAsync($"{_settings.BaseUrl}/timelogs?me&trackedDate={DateTime.UtcNow:yyyy-MM-dd}");
        //        response.EnsureSuccessStatusCode();
        //        return await response.Content.ReadFromJsonAsync<TimelogsResponse>();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error while getting todays time logs.");
        //        return null;
        //    }
        //}

        public async Task<TimelogsResponse> GetWeeklyTimeLogs(string accessToken)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var today = DateTime.UtcNow;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                var endOfWeek = startOfWeek.AddDays(6);
                var response = await _client.GetAsync($"{_settings.BaseUrl}/timelogs?me&trackedDate={{\"start\":\"{startOfWeek:yyyy-MM-dd}\", \"end\":\"{endOfWeek:yyyy-MM-dd}\"}}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TimelogsResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting weekly time logs.");
                return null;
            }
        }

        public async Task<TimelogsResponse> GetTodaysTimelogs(string accessToken)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.GetAsync($"{_settings.BaseUrl}/timelogs?me&trackedDate={DateTime.UtcNow:yyyy-MM-dd}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TimelogsResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting todays task specific time logs.");
                return null;
            }
        }

        public async Task<TimelogsResponse> GetLastMonthTimelogs(string accessToken)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var dateTimeNowWrikeFormat = DateTime.UtcNow.AddMonths(-1).ToString("yyyy/MM/dd").Replace("/", "-");
                var response = await _client.GetAsync($"{_settings.BaseUrl}/timelogs?me&trackedDate={{\"start\":\"{dateTimeNowWrikeFormat}\"}}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TimelogsResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting lastMonth's time logs.");
                return null;
            }
        }

        public async Task<TaskResponse> GetTaskDetailsAsync(string accessToken, string taskId)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.GetAsync($"{_settings.BaseUrl}/tasks/{taskId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TaskResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting task details.");
                return null;
            }
        }

        public async Task<TimelogsResponse> GetSpecificTimelogWithTimelogIdAsync(string accessToken, string timelogId)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.GetAsync($"{_settings.BaseUrl}/timelogs/{timelogId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TimelogsResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting specific timelog with timelogId.");
                return null;
            }
        }

        public async Task DeleteTimeLogAsync(string accessToken, string timelogsId)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.DeleteAsync($"{_settings.BaseUrl}/timelogs/{timelogsId}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting time log.");
            }
        }

        public async Task LogTimeAsync(string accessToken, string taskId, double hours, string trackedDate)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var requestData = new Dictionary<string, string>
                {
                    ["hours"] = hours.ToString(),
                    ["trackedDate"] = trackedDate
                };
                var response = await _client.PostAsync($"{_settings.BaseUrl}/tasks/{taskId}/timelogs", new FormUrlEncodedContent(requestData));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while logging time.");
            }
        }


        public async Task<TaskResponse> GetTaskFromPermalink(string accessToken, string wrikePermaLinkId)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var permaLink = "https://www.wrike.com/open.htm?id=" + wrikePermaLinkId;
                var response = await _client.GetAsync($"{_settings.BaseUrl}/tasks?permalink={permaLink}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TaskResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting task from permalink.");
                return null;
            }

            //todo:make sure it works as intended 
        }

        public async Task<Webhook> GetWebhooksAsync(string accessToken)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.GetAsync($"{_settings.BaseUrl}/webhooks");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<Webhook>(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting webhooks.");
                return null;
            }
        }

        public async Task<Webhook> DeleteWebhookAsync(string accessToken, string webhookId)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _client.DeleteAsync($"{_settings.BaseUrl}/webhooks/{webhookId}");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<Webhook>(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting webhook.");
                return null;
            }
        }

        public async Task<Webhook> CreateWebhookAsync(string accessToken)
        {
            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var requestData = new
                {
                    hookUrl = $"{_settings.WebhookUrl}",
                    events = new[] { "TaskStatusChanged", "TaskResponsiblesAdded", "TaskResponsiblesRemoved", "TaskDeleted" }  //"TimelogChanged",
                };

                var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _client.PostAsJsonAsync($"{_settings.BaseUrl}/webhooks", requestData);
                var responseBody = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                return System.Text.Json.JsonSerializer.Deserialize<Webhook>(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating webhook.");
                return null;
            }
        }
    }
}
