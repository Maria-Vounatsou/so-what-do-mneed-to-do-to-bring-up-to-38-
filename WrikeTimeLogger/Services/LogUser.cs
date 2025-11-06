using PeopleCert.Extensions.Logging;
using System.Collections.Generic;

namespace WrikeTimeLogger.Services
{
    internal sealed class LogsUser(IHttpContextAccessor contextAccessor) : ILoggerContextProvider
    {
        public void GetPropertiesFromContext(LogEntry logEntry)
        {
            if (contextAccessor.HttpContext is null || contextAccessor.HttpContext.User?.Identity?.IsAuthenticated == false)
            {
                return;
            }

            logEntry.Properties["userName"] = contextAccessor.HttpContext.User?.Identity?.Name;
            //logEntry.Properties["role"] = user.Role;
        }
    }
}
