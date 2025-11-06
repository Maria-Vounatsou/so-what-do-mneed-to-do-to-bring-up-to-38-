using Microsoft.EntityFrameworkCore;
using WrikeTimeLogger.Data;

namespace WrikeTimeLogger.Services
{
    public class DeleteWorker : BackgroundService
    {
        private readonly ILogger<TimelogWorker> _logger;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DeleteWorker(ILogger<TimelogWorker> logger, IDbContextFactory<AppDbContext> dbContext)
        {
            _logger = logger;
            _dbContextFactory = dbContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Friday && DateTime.Now.Hour >= 18 && DateTime.Now.Minute >= 30)
                {
                    try
                    {
                        var compare = DateTime.UtcNow.AddDays(-2);
                        using var dbContext = _dbContextFactory.CreateDbContext();
                        var hoursToDelete = dbContext.HoursToAdd.Where(e => e.DateIn <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))).ToList();
                        dbContext.HoursToAdd.RemoveRange(hoursToDelete);
                        var webhooksToDelete = dbContext.Webhooks.AsEnumerable().Where(e => DateTime.Parse(e.lastUpdatedDate!).Date <= compare).ToList();
                        dbContext.Webhooks.RemoveRange(dbContext.Webhooks);
                        var errorLogsToDelete = dbContext.ErrorLogs.Where(e => e.CreatedAt <= DateTime.UtcNow.AddDays(-14)).ToList();
                        dbContext.ErrorLogs.RemoveRange(errorLogsToDelete);
                            
                        //var tasksToBeRemoved = dbContext.UsersTasks.Where(x => x.Status == "Live" || x.Status == "Completed");
                        //dbContext.UsersTasks.RemoveRange(tasksToBeRemoved);

                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("All data has been deleted");

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in DeleteWorker");
                    }
                }
                await Task.Delay(1200000, stoppingToken); //20 min
            }
        }
    }
}
