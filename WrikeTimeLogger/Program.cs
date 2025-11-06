using WrikeTimeLogger.Components;
using WrikeTimeLogger.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using PeopleCert.Extensions.Logging;
using PeopleCert.Extensions.Logging.EntityFrameworkCore;
using WrikeTimeLogger.Data;
using WrikeTimeLogger.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Blazored.Modal;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddDbContextFactory<AppDbContext>(
    options =>
    {
        options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"));

    });

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddHttpClient<WrikeService>();
builder.Services.AddScoped<LogService>();
//builder.Services.AddScoped<Webhooks>();
//builder.Services.AddScoped<UsersTasksService>();
builder.Services.AddBlazoredModal();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<TimelogWorker>();
builder.Services.AddHostedService<DeleteWorker>();
builder.Services.AddHostedService<WebhookWorker>();
builder.Services.AddHttpClient<MailServices>();
builder.Logging.AddConsole();

builder.Services.AddPeopleCertLogger()
    .AddEnricher<LogsUser>()
    .Build(builder.Configuration);

builder.Services.AddSingleton<ILogsStore, EntityFrameworkLogsStore<ErrorLog, AppDbContext>>(sp
            => new EntityFrameworkLogsStore<ErrorLog, AppDbContext>(sp.GetRequiredService<IServiceScopeFactory>(), static entry =>
            {
                entry.Properties.TryGetValue("userName", out var userName);

                return new ErrorLog
                {
                    Exception = entry.Exception,
                    SourceContext = entry.SourceContext,
                    WrikeId = userName?.ToString(),
                    LogLevel = entry.Level,
                    CreatedAt = DateTime.UtcNow,
                    Template = entry.Template,
                };
            }));

builder.Services.Configure<ClientSettings>(builder.Configuration.GetSection("Wrike"));


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/Error";
        //options.Events.OnRedirectToAccessDenied = context =>
        //{
        //    context.Response.StatusCode = 403;
        //    return Task.CompletedTask;
        //};
        options.LoginPath = "/login";
        // options.LogoutPath = "/logout";
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var wrikeService = scope.ServiceProvider.GetRequiredService<WrikeService>();
await wrikeService.LoadWorkFlows();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.MapGet("/Wrike-SignIn", WrikeEndPoints.SignIn);

app.MapPost("/api/wrike/webhook", WrikeEndPoints.HandleWebhooksAsync);

// GET /api/support-tasks  → read rows
app.MapGet("/api/support-tasks", async (AppDbContext db) =>
    await db.SupportTasks
        .OrderByDescending(t => t.UpdatedAt)
        .Select(t => new
        {
            t.Id,
            t.TaskId,
            Link = t.Link ?? $"https://www.wrike.com/open.htm?id={t.TaskId}",
            t.Title,
            t.TeamName,
            t.Completed,
            t.ReassignedAway,
            t.UpdatedAt
        })
        .ToListAsync());

// POST /api/support-tasks/seed  → insert 1 dummy row (for testing only)
app.MapPost("/api/support-tasks/seed", async (AppDbContext db) =>
{
    var row = new SupportTask
    {
        TaskId = Guid.NewGuid().ToString("N")[..12],
        Title = "Dummy local task — safe to delete",
        Description = "Seeded to test EF + DB",
        TeamName = ""
    };
    db.SupportTasks.Add(row);
    await db.SaveChangesAsync();
    return Results.Created($"/api/support-tasks/{row.Id}", new { row.Id });
});

app.Run();

