using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WrikeTimeLogger.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Azure.Core;
using Microsoft.EntityFrameworkCore.Storage;
using WrikeTimeLogger.Models;
using System.Net;

namespace WrikeTimeLogger.Services;

public sealed class WrikeEndPoints
{
    public static async Task<RedirectHttpResult> SignIn(HttpContext context, string code, WrikeService wrikeService, IDbContextFactory<AppDbContext> dbContext, ILogger<WrikeEndPoints> logger)
    {
        var response = await wrikeService.GetTokensAsync(code);
        if (response == null) { Results.Redirect("/login"); }
        try
        {
            var data = await wrikeService.GetWrikeIdAsync(response!.AccessToken);

            response.WrikeId = data.data[0].id;
            response.Name = data.data[0].firstName + " " + data.data[0].lastName;
            response.Email = data.data[0].profiles[0].email;

            using (var appDbContext = dbContext.CreateDbContext())
            {
                var existingUser = await appDbContext.Users.FirstOrDefaultAsync(u => u.WrikeId == response.WrikeId);

                if (existingUser != null)
                {
                    response.Role = existingUser.Role;
                    existingUser.AccessToken = response.AccessToken;
                    existingUser.RefreshToken = response.RefreshToken;
                    existingUser.ExpiresIn = response.ExpiresIn;
                    existingUser.Role = response.Role;
                    existingUser.Name = response.Name;
                    existingUser.Email = response.Email;
                }
                else
                {
                    if (response.WrikeId == "KUASAY6A" || response.WrikeId == "KUAR6M2S" || response.WrikeId == "KUABWCBG" || response.WrikeId == "KUABWKBA")
                    {
                        response.Role = "Admin";
                    }
                    else
                    {
                        response.Role = "User";
                    }
                    appDbContext.Users.Add(response);
                }

                await appDbContext.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, response.WrikeId),
                    new Claim(ClaimTypes.GivenName, response.Name),
                    new Claim(ClaimTypes.Role, response.Role)
                };

                var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await context.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);
            }
            return TypedResults.Redirect("/");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"An error occurred while processing the Wrike-SignIn request for the user with wrikeId:{response!.WrikeId}");
            return TypedResults.Redirect("/login");

        }
    }

    public static async Task<IResult> HandleWebhooksAsync(WebhookPayload[] payload, HttpRequest request, WrikeService wrikeService, IDbContextFactory<AppDbContext> dbContext, ILogger<WrikeEndPoints> logger)
    {
        using var scope = logger.BeginScope("Webhook Event");
        var idempotencyKey = request.Headers["Idempotency-Key"].FirstOrDefault();
        using (var appDbContext = dbContext.CreateDbContext())
        {
            var webhooks = await appDbContext.Webhooks.Where(x => x.idempotencyKey == idempotencyKey).FirstOrDefaultAsync();
            using (IDbContextTransaction transaction = appDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (webhooks == null)
                    {
                        payload[0].idempotencyKey = idempotencyKey!;
                        appDbContext.Webhooks.Add(payload[0]);
                        await appDbContext.SaveChangesAsync();

                        var exists = await appDbContext.UsersTasks
                            .Where(ut => ut.TaskId == payload[0].taskId)
                            .Select(ut => ut.TaskId)
                            .Union(
                                 appDbContext.Users
                                .Where(u => u.WrikeId == payload[0].eventAuthorId)
                                .Select(u => u.WrikeId)
                            ).AnyAsync();

                        if (exists)
                        {
                            var user = await appDbContext.Users.FirstOrDefaultAsync(u => u.WrikeId == payload[0].eventAuthorId && u.IsEnabled);
                            if (user == null)
                            {
                                var usersTasks = await appDbContext.UsersTasks.Where(ut => ut.TaskId == payload[0].taskId).FirstOrDefaultAsync();
                                if (usersTasks == null) return Results.StatusCode((int)HttpStatusCode.NoContent);
                                user = await appDbContext.Users.Where(u => u.WrikeId == usersTasks.UserId && u.IsEnabled).FirstOrDefaultAsync();
                                if (user == null) return Results.StatusCode((int)HttpStatusCode.NoContent);
                            };
                            var accessToken = await wrikeService.EnsureValidAccessTokenAsync(user!.AccessToken, user.WrikeId);
                            if(string.IsNullOrEmpty(accessToken)) return Results.StatusCode((int)HttpStatusCode.NoContent);
                            var task = await wrikeService.GetTaskDetailsAsync(accessToken, payload[0].taskId!);

                            WrikeService.WorkflowsIds.TryGetValue(task.data[0].customStatusId, out MyWorkFlows? workflowId);

                            var customStatusName = workflowId?.customStatusName;
                            var workflowName = workflowId?.workflowName;

                            switch (payload[0].eventType)
                            {
                                case "TaskStatusChanged":

                                    foreach (var responsible in task.data[0].responsibleIds)
                                    {
                                        if (await appDbContext.Users.AnyAsync(ut => ut.WrikeId == responsible))
                                        {
                                            var userTask = await appDbContext.UsersTasks.Where(ut => ut.TaskId == payload[0].taskId && ut.UserId == responsible).FirstOrDefaultAsync();

                                            if (userTask == null)
                                            {

                                                appDbContext.UsersTasks.Add(new UsersTasks
                                                {
                                                    UserId = responsible!,
                                                    TaskId = payload[0].taskId!,
                                                    Status = payload[0].status!,
                                                    Workflow = workflowName!,
                                                });

                                            }
                                            else
                                            {
                                                userTask.DateUpt = DateTime.UtcNow;
                                                userTask.Status = payload[0].status!;
                                                userTask.Workflow = workflowName!;
                                            }
                                        }
                                    }
                                    break;

                                case "TaskResponsiblesAdded":

                                    foreach (var responsible in payload[0].addedResponsibles!)
                                    {
                                        if (await appDbContext.Users.AnyAsync(x => x.WrikeId == responsible))
                                        {
                                            if (!await appDbContext.UsersTasks.AnyAsync(x => x.TaskId == payload[0].taskId && x.UserId == responsible))
                                            {
                                                appDbContext.UsersTasks.Add(new UsersTasks
                                                {
                                                    UserId = responsible,
                                                    TaskId = payload[0].taskId!,
                                                    Status = customStatusName!,
                                                    Workflow = workflowName!,
                                                });
                                            }
                                            else
                                            {
                                                var userTask = await appDbContext.UsersTasks.FirstOrDefaultAsync(u => u.UserId == responsible && u.TaskId == payload[0].taskId);
                                                userTask!.DateUpt = DateTime.UtcNow;
                                            }
                                        }
                                    }
                                    break;

                                case "TaskResponsiblesRemoved":
                                    foreach (var responsible in payload[0].removedResponsibles!)
                                    {
                                        var userTask = await appDbContext.UsersTasks.FirstOrDefaultAsync(u => u.UserId == responsible && u.TaskId == payload[0].taskId);
                                        if (userTask != null)
                                        {
                                            appDbContext.UsersTasks.Remove(userTask!);
                                        }
                                    }
                                    break;

                                case "TaskDeleted":
                                    foreach (var responsible in task.data[0].responsibleIds)
                                    {
                                        var userTasks = await appDbContext.UsersTasks.FirstOrDefaultAsync(ut => ut.TaskId == payload[0].taskId && ut.UserId == responsible);
                                        if (userTasks != null)
                                        {
                                            appDbContext.UsersTasks.Remove(userTasks);
                                        }
                                    }
                                    break;

                                    //case "TimelogChanged":
                                    //    if (payload[0].type == "Added")
                                    //    {
                                    //        var specificTimelog = await wrikeService.GetSpecificTimelogWithTimelogIdAsync(accessToken, payload[0].timeTrackerId!);
                                    //        DateOnly dateOnly;
                                    //        bool isValidDate = DateOnly.TryParse(specificTimelog.Data[0].TrackedDate, out dateOnly);

                                    //        if (isValidDate)
                                    //        {
                                    //            appDbContext.TimeTrackers.Add(new TimeTracker
                                    //            {
                                    //                userId = payload[0].eventAuthorId!,
                                    //                hours = double.Parse(payload[0].hours!),
                                    //                taskId = payload[0].taskId!,
                                    //                timeTrackerId = payload[0].timeTrackerId!,
                                    //                date = dateOnly
                                    //            });
                                    //        }
                                    //    }
                                    //    else if (payload[0].type == "Removed")
                                    //    {
                                    //        //Todo check if the timelog exists
                                    //        var timeTracker = await appDbContext.TimeTrackers.Where(t => t.timeTrackerId == payload[0].timeTrackerId).FirstOrDefaultAsync();
                                    //        if (timeTracker != null)
                                    //        {
                                    //            appDbContext.TimeTrackers.Remove(timeTracker);
                                    //        }
                                    //    }
                                    //    await appDbContext.SaveChangesAsync();
                                    //    break;
                            }
                            await appDbContext.SaveChangesAsync();
                            await transaction.CommitAsync();
                            return Results.Ok();
                        }
                        else
                        {
                            //TODO: Check how this works
                            await transaction.CommitAsync();
                            return Results.StatusCode(204);
                        }
                    }
                    else
                    {
                        await transaction.CommitAsync();
                        return Results.StatusCode(204);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred while processing the webhook event: {payload[0].eventType} for TaskId: {payload[0].taskId}");
                    await transaction.RollbackAsync();
                    return Results.StatusCode(500);
                }
            }
        }
    }
}
