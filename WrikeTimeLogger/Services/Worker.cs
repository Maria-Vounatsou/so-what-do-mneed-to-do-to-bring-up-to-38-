using Microsoft.EntityFrameworkCore;
using WrikeTimeLogger.Data;
using WrikeTimeLogger.Models;

namespace WrikeTimeLogger.Services
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContext;
        private readonly MailServices _mailServices;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IDbContextFactory<AppDbContext> dbContext, MailServices mailServices)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _dbContext = dbContext;
            _mailServices = mailServices;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogError("The worker stopped working.");
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday || ( DateTime.Now.Hour < 9 && (DateTime.Now.Hour == 9 && DateTime.Now.Minute <= 30)))
                {
                    await Task.Delay(900000); // 15 min 2700000
                    continue;
                }
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var wrikeService = scope.ServiceProvider.GetRequiredService<WrikeService>();
                    using var dbContext = _dbContext.CreateDbContext();

                    await WorkerProccess(wrikeService, dbContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the worker service.");
                }
                await Task.Delay(3300000); // 55mins 2700000
            }
        }

        private async Task WorkerProccess(WrikeService wrikeService, AppDbContext dbContext)
        {
            var usersToCheck = new List<User>();
            var dateTimeCorrectFormat = DateOnly.FromDateTime(DateTime.UtcNow);
            var users = await dbContext.Users
                .Where(e=> e.IsEnabled != false)
                .AsNoTracking()
                .ToListAsync();
            var accessToken = string.Empty;
            foreach (var user in users)
            {
                try
                {
                    accessToken = await wrikeService.EnsureValidAccessTokenAsync(user.AccessToken, user.WrikeId);
                    if (string.IsNullOrEmpty(accessToken)) return;
                    var timeToCheck = await wrikeService.GetTodaysTimelogs(accessToken);
                    if (timeToCheck != null)
                    {
                        var timeToCheckHours = timeToCheck.Data.Sum(x => x.Hours);

                        if (timeToCheckHours < 8)
                        {
                            usersToCheck.Add(user);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred in WorkerProccess for user {user.WrikeId} on users loop");
                }
            }

            foreach (var user in usersToCheck)
            {
                try 
                {
                    if (await dbContext.HoursToAdd.Where(h => h.WrikeId == user.WrikeId && h.DateIn == dateTimeCorrectFormat).CountAsync() >= 8) continue;

                    var automatedTask = await dbContext.UsersTasks.Where(ut => ut.UserId == user.WrikeId && ut.IsAutomated == true).FirstOrDefaultAsync();
                    if (automatedTask != null)
                    {
                        var dbEntry = new HoursToAdd
                        {
                            TaskId = automatedTask.TaskId,
                            Hours = 1,
                            WrikeId = user.WrikeId,
                            DateIn = dateTimeCorrectFormat
                        };
                        dbContext.HoursToAdd.Add(dbEntry);
                    }
                    else
                    {
                        var tasksInDb = await dbContext.UsersTasks
                        //todo fix this for new workflows
                        .Where(t => t.UserId == user.WrikeId &&
                        ((t.Workflow == "Assessment Management Systems" && t.Status == "In Progress") ||
                        (t.Workflow == "Workflow 1" && (t.Status == "Development" || t.Status == "In Progress")) ||
                        (t.Workflow == "Kanban 2" && t.Status == "Doing") ||
                        (t.Workflow == "Kanban" && t.Status == "Doing") ||
                        (t.Workflow == "Main Workflow" && (t.Status == "In Progress" || t.Status == "Active")) ||
                        (t.Workflow == "CxP Workflow" && t.Status == "In Progress")))
                        .OrderByDescending(t => t.DateUpt ?? t.DateIn)
                        .AsNoTracking()
                        .ToListAsync();

                        if (tasksInDb.Count() == 0)
                        {
                            var pluralSight = await dbContext.UsersTasks
                                .Where(t => t.UserId == user.WrikeId &&
                                t.TaskId == "IEAAYS36KRH46EA4")
                                .AsNoTracking()
                                .FirstOrDefaultAsync();

                            if (pluralSight == null) continue;

                            var DbEntry = new HoursToAdd
                            {
                                TaskId = pluralSight.TaskId,
                                Hours = 1,
                                WrikeId = pluralSight.UserId,
                                DateIn = dateTimeCorrectFormat
                            };
                            dbContext.HoursToAdd.Add(DbEntry);
                        }
                        else if (tasksInDb.Count() == 1)
                        {
                            var DbEntry = new HoursToAdd
                            {
                                TaskId = tasksInDb[0].TaskId,
                                Hours = 1,
                                WrikeId = user.WrikeId,
                                DateIn = dateTimeCorrectFormat
                            };
                            dbContext.HoursToAdd.Add(DbEntry);
                        }
                        else
                        {
                            accessToken = await wrikeService.EnsureValidAccessTokenAsync(user.AccessToken, user.WrikeId);
                            var lastMonthsTimelogs = await wrikeService.GetLastMonthTimelogs(accessToken);
                            //Todo send emails to users that have no timelogs
                            if (lastMonthsTimelogs == null)
                            {
                                //var sendMail = await _mailServices.SendEmailAsync("No timelogs found", $"No timelogs found for {user.Name} in the last month, either make a first entry via manually logging hours or select automation for a task.", 2, user.Email, "mail_send");
                                continue;
                            }

                            lastMonthsTimelogs.Data = [.. lastMonthsTimelogs.Data.OrderByDescending(t => t.CreatedDate)];


                            var taskToEnterHoursToAdd = tasksInDb.Where(t => t.TaskId == lastMonthsTimelogs.Data[0].TaskId).FirstOrDefault();

                            if (taskToEnterHoursToAdd == null)
                            {
                                var DbEntry = new HoursToAdd
                                {
                                    TaskId = tasksInDb.First().TaskId,
                                    Hours = 1,
                                    WrikeId = user.WrikeId,
                                    DateIn = dateTimeCorrectFormat
                                };
                                dbContext.HoursToAdd.Add(DbEntry);
                            }
                            else
                            {
                                var DbEntry = new HoursToAdd
                                {
                                    TaskId = taskToEnterHoursToAdd.TaskId,
                                    Hours = 1,
                                    WrikeId = user.WrikeId,
                                    DateIn = dateTimeCorrectFormat
                                };
                                dbContext.HoursToAdd.Add(DbEntry);
                            }
                        }
                    }
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, $"An error occurred in WorkerProccess for user {user.WrikeId} on usersToCheck loop");
                }
            }
            await dbContext.SaveChangesAsync();
        }
    }
}