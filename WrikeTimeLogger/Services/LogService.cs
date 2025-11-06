using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net.Http;
using WrikeTimeLogger.Components.Layout;
using WrikeTimeLogger.Data;
using WrikeTimeLogger.Models;

namespace WrikeTimeLogger.Services
{
    public class LogService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContext;

        public LogService(IDbContextFactory<AppDbContext> dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<DateGroupedLogs> GetAllLogsGroupedByDateAsync()
        {
            using var dbContext = _dbContext.CreateDbContext();


            var logsGroupedByDate =  dbContext.ErrorLogs
                .GroupBy(log => log.CreatedAt.Date)
                .Select(group => new DateGroupedLogs
                {
                    Date = group.Key,
                    Logs = group.OrderByDescending(log => log.CreatedAt).ToList()
                })
                .OrderByDescending(g => g.Date)
                .ToList();

            return logsGroupedByDate;
        }

        //internal static async Task<Ok<List<LogFilters>>> GetFilterPredicateAsync(
        //    [AsParameters] Filters filters,
        //    [AsParameters] CursorBasedPageRequest<int> page,
        //    [FromServices] AppDbContext dbContext)
        //{
        //    page.Validate();

          

        //    return TypedResults.Ok(logs);
        //}
    }

    public class DateGroupedLogs
    {
        public DateTime Date { get; set; }
        public List<ErrorLog> Logs { get; set; }
    }
}
