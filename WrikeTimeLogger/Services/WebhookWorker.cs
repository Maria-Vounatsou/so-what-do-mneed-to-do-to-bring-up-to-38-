
using Microsoft.EntityFrameworkCore;
using WrikeTimeLogger.Data;

namespace WrikeTimeLogger.Services
{
    public class WebhookWorker: BackgroundService
    {
        private readonly ILogger<WebhookWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContext;

        public WebhookWorker(ILogger<WebhookWorker> logger, IServiceProvider serviceProvider, IDbContextFactory<AppDbContext> dbContext)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _dbContext = dbContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAndRecreateWebhooksAsync();

                await Task.Delay(300000, stoppingToken);
            }
        }

        private async Task CheckAndRecreateWebhooksAsync()
        {     
            using var scope = _serviceProvider.CreateScope();
            var wrikeService = scope.ServiceProvider.GetRequiredService<WrikeService>();
            using var appDbContext = _dbContext.CreateDbContext();
            var adminUsers = await appDbContext.Users.Where(x => x.Role == "Admin").AsNoTracking().ToListAsync();
            if(adminUsers.Count == 0)
            {
                _logger.LogError("No admin users found. Exiting the worker!");
                return;
            }

            foreach(var admin in adminUsers)
            {
                if (!string.IsNullOrEmpty(admin.AccessToken))
                {
                    try
                    {
                        var accessToken = await wrikeService.EnsureValidAccessTokenAsync(admin.AccessToken, admin.WrikeId);
                        var webhooks = await wrikeService.GetWebhooksAsync(accessToken);
                        if (webhooks == null || webhooks.data.Count() == 0) continue;
                        foreach (var webhook in webhooks.data)
                        {
                            if (webhook.status == "Suspended")
                            {
                                await wrikeService.DeleteWebhookAsync(accessToken, webhook.id);
                                await wrikeService.CreateWebhookAsync(accessToken);
                            }  
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Error on the webhook worker!");
                        return;
                    }
                }
            }
        }
    }
}
