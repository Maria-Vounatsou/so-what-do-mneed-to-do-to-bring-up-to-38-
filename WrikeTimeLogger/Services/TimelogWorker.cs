using Microsoft.EntityFrameworkCore;
using WrikeTimeLogger.Data;
using WrikeTimeLogger.Models;

namespace WrikeTimeLogger.Services
{
    public class TimelogWorker : BackgroundService
    {
        private readonly ILogger<TimelogWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContext;

        public TimelogWorker(ILogger<TimelogWorker> logger, IServiceProvider serviceProvider, IDbContextFactory<AppDbContext> dbContext)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _dbContext = dbContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday || (DateTime.Now.Hour < 17 || (DateTime.Now.Hour == 17 && DateTime.Now.Minute < 15)))
                {
                    await Task.Delay(900000); //30min 1800000
                    continue;
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var wrikeService = scope.ServiceProvider.GetRequiredService<WrikeService>();
                    using var dbContext = _dbContext.CreateDbContext();

                    var users = await dbContext.Users
                        .Where(e=> e.IsEnabled != false)
                        .AsNoTracking()
                        .ToListAsync();
                    if(users.Count == 0)
                    {
                        _logger.LogError("No users found to process.");
                        await Task.Delay(1800000); //30min 1800000
                        continue;
                    }
                    foreach (var user in users)
                    {
                        await TimeProccessUser(wrikeService, dbContext, user);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in TimelogWorker");
                }
                await Task.Delay(1800000); //30min 1800000
            } 
        }

        private async Task TimeProccessUser(WrikeService wrikeService, AppDbContext dbContext, User? user)
        {
            user.AccessToken = await wrikeService.EnsureValidAccessTokenAsync(user.AccessToken, user.WrikeId);

            var todaysTimelogs = await wrikeService.GetTodaysTimelogs(user.AccessToken);

            var wrikeLoggedTodayHours = todaysTimelogs.Data.Sum(wl => wl.Hours);
            if (wrikeLoggedTodayHours >= 8) return;
            else if (wrikeLoggedTodayHours>7)
            {
                var latestTaskId = todaysTimelogs.Data.OrderByDescending(x => x.TrackedDate).First().TaskId;
                var additionalHour = 8 - wrikeLoggedTodayHours;
                await wrikeService.LogTimeAsync(user.AccessToken, latestTaskId, additionalHour, DateTime.UtcNow.ToString("yyyy-MM-dd"));
                return;
            }
            else
            {
                var dateTimeCorrectFormat = DateOnly.FromDateTime(DateTime.UtcNow);

                var todaysHoursToAdd = await dbContext.HoursToAdd
                    .Where(x => x.WrikeId == user.WrikeId && x.DateIn == dateTimeCorrectFormat)
                    .OrderByDescending(x => x.DateIn)
                    .ToListAsync();
                var totalHoursInHoursToAdd = todaysHoursToAdd.Sum(x => x.Hours);

                if (totalHoursInHoursToAdd <= 0) return;

                var backUpCounter = 0;
                //ToDo: round the last hour to log
                while (wrikeLoggedTodayHours + totalHoursInHoursToAdd > 8)
                {
                    foreach (var ttc in todaysTimelogs.Data)
                    {
                        var matchingItem = todaysHoursToAdd.FirstOrDefault(t => t.TaskId == ttc.TaskId);
                        if (matchingItem != null)
                        {
                            matchingItem.Hours -= (int)Math.Ceiling(ttc.Hours);

                            if (matchingItem.Hours == 0)
                            {
                                todaysHoursToAdd.Remove(matchingItem);
                            }
                            else if (matchingItem.Hours > 0)
                            {
                                todaysHoursToAdd.Where(t => t.TaskId == ttc.TaskId).First().Hours = matchingItem.Hours;
                            }
                            else
                            {
                                var endIndex = (int)Math.Abs(matchingItem.Hours);

                                var localHours=todaysHoursToAdd.Where(t=>t.TaskId == ttc.TaskId).ToList();
                                if(endIndex == localHours.Count)
                                {                                
                                    todaysHoursToAdd.RemoveAll(t=>t.TaskId == ttc.TaskId);
                                }
                                else if (endIndex > localHours.Count)
                                {
                                    todaysHoursToAdd.RemoveAll(t => t.TaskId == ttc.TaskId);
                                    todaysHoursToAdd.RemoveRange(0, endIndex - localHours.Count);
                                }
                                else
                                {
                                    for(int i=0; i<=endIndex; i++)
                                    {
                                        todaysHoursToAdd.Remove(localHours[i]);
                                    }
                                }                             
                            }
                        }
                        else
                        {
                            var endIndex = (int)Math.Ceiling(ttc.Hours);
                            todaysHoursToAdd.RemoveRange(0, endIndex );
                        }
                        totalHoursInHoursToAdd = todaysHoursToAdd.Sum(x => x.Hours);
                        backUpCounter++;
                        if (backUpCounter > 20)
                        {
                            break;
                        }
                    }
                }
                if (totalHoursInHoursToAdd == 0) return;
                else
                {
                    if (todaysHoursToAdd.All(x => x.TaskId == todaysHoursToAdd[0].TaskId))
                    { //TODO: ean sthn prwth kataxorisi exei 3 px items sto hoursToAdd kanei 3wrh kataxorisi sthn epomenh loupa tha xrhsimopoihsei ta idia !!!!!
                        await wrikeService.LogTimeAsync(user.AccessToken, todaysHoursToAdd[0].TaskId, totalHoursInHoursToAdd, DateTime.UtcNow.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        //TODO: change the data on the hourstoadd to take DateTime rather than DateOnly and then group by TaskId and Date
                        //you do this so we can check and maybe catch if you have any hours logged for the same task on the same day already we should not log them again
                        var taskOccurences = todaysHoursToAdd
                            .GroupBy(t => t.TaskId)
                            .Select(group => new
                            {
                                TaskId = group.Key,
                                LoggedHoursForTask = group.Sum(x => x.Hours),
                            })
                            .ToList();

                        foreach (var task in taskOccurences)
                        {
                            await wrikeService.LogTimeAsync(user.AccessToken, task.TaskId, task.LoggedHoursForTask, DateTime.UtcNow.ToString("yyyy-MM-dd"));
                        }
                    }
                }
            }
            var safemeasure = await wrikeService.GetTodaysTimelogs(user.AccessToken);
            if (safemeasure != null)
            {
                if (safemeasure.Data.Sum(t => t.Hours) != 8 && DateTime.Now.Hour < 19 && DateTime.Now.Minute < 30)
                {
                    //ToDO: if it stays mail

                    //if not 8 hours and user is on your team put the difference from the 8 on pluralsight


                    //await wrikeService.LogTimeAsync(user.AccessToken, , 8 - safemeasure.Data.Sum(t => t.Hours), DateTime.UtcNow.ToString("yyyy-MM-dd"));
                }
            }
            else
            {
                _logger.LogInformation($"No timelogs for user {user.WrikeId} today and should send email that its not 8 hours");
            }
        }
    }
}