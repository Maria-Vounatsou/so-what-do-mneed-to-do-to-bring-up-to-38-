using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WrikeTimeLogger.Services;
using WrikeTimeLogger.Models;

namespace WrikeTimeLogger.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<WebhookPayload> Webhooks { get; set; }
        public DbSet<UsersTasks> UsersTasks { get; set; }
        public DbSet<HoursToAdd> HoursToAdd { get; set; }
        public DbSet<TimeTracker> TimeTrackers { get; set; }
        public DbSet<SupportTask> SupportTasks { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(t => t.WrikeId);
                entity.Property(t => t.WrikeId).ValueGeneratedNever().HasMaxLength(50);
                entity.Property(t => t.AccessToken).HasMaxLength(4000);
                entity.Property(t => t.RefreshToken).HasMaxLength(4000);
            });

            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.WrikeId).HasMaxLength(50);
                entity.Property(entity => entity.SourceContext).HasMaxLength(300);
                entity.Property(t => t.LogLevel).HasMaxLength(6000);
                entity.Property(t => t.Exception).HasMaxLength(6000);
            });

            modelBuilder.Entity<WebhookPayload>(entity =>
            {
                entity.HasKey(t => t.id);
                entity.Property(t => t.oldStatus).HasMaxLength(50);
                entity.Property(t => t.status).HasMaxLength(50);
                entity.Property(t => t.oldCustomStatusId).HasMaxLength(50);
                entity.Property(t => t.customStatusId).HasMaxLength(50);
                entity.Property(t => t.taskId).HasMaxLength(50);
                entity.Property(t => t.webhookId).HasMaxLength(50);
                entity.Property(t => t.eventAuthorId).HasMaxLength(50);
                entity.Property(t => t.eventType).HasMaxLength(50);
                entity.Property(t => t.lastUpdatedDate).HasMaxLength(50);
            });

            modelBuilder.Entity<UsersTasks>(entity => 
            {
                entity.HasKey(ut => new { ut.UserId, ut.TaskId });
                entity.Property(t => t.UserId).HasMaxLength(50);
                entity.Property(t => t.TaskId).HasMaxLength(50);
                entity.Property(t => t.Workflow).HasMaxLength(50);
                entity.HasOne(u => u.User)
                     .WithMany() // Assuming Users have a collection of UsersTasks
                     .HasForeignKey(ut => ut.UserId)
                     .HasPrincipalKey(u => u.WrikeId) // Assuming WrikeId is the primary key in Users table
                     .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<HoursToAdd>(entity => 
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.TaskId).HasMaxLength(50);
                entity.Property(t => t.WrikeId).HasMaxLength(50);
                entity.HasOne<UsersTasks>()
                     .WithMany() // Configure if HoursToAdd has a collection in UsersTasks
                     .HasForeignKey(h => new { h.WrikeId, h.TaskId })
                     .HasPrincipalKey(u => new { u.UserId, u.TaskId })
                     .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TimeTracker>(entity =>
            {
                entity.HasKey(t => t.id);
                entity.Property(t => t.userId).HasMaxLength(50);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(t => t.userId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(dateTimeConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableDateTimeConverter);
            }

        }
    }
}
