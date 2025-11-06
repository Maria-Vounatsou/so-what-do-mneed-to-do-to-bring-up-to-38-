//using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.EntityFrameworkCore;
//using System.ComponentModel;
//using WrikeTimeLogger.Data;
//using WrikeTimeLogger.Models;

//namespace WrikeTimeLogger.Services
//{
//    public class Users
//    {
//        private readonly IDbContextFactory<AppDbContext> _dbContext;
//        private readonly ILogger<WrikeService> _logger;

//        public AuthenticationStateProvider _authenticationStateProvider { get; }

//        public Users(IDbContextFactory<AppDbContext> dbContext, AuthenticationStateProvider AuthenticationStateProvider, ILogger<WrikeService> logger)
//        {
//            _dbContext = dbContext;
//            _authenticationStateProvider = AuthenticationStateProvider;
//            _logger = logger;
//        }

//        public async Task<User> GetSavedTokensAsync()
//        {
//            try
//            {
//                using (var dbContext = _dbContext.CreateDbContext())
//                {
//                    var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
//                    var user = authState.User;
//                    var tokens = await dbContext.Users
//                        .Where(t => t.WrikeId == user.Identity!.Name)
//                        .AsNoTracking()
//                        .FirstOrDefaultAsync();
//                    return tokens!;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An error occurred while getting the saved tokens from Users service.");
//                return null!;
//            }
//        }
//    }
//}
