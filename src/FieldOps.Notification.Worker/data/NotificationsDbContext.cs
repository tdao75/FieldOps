using FieldOps.Notification.Worker.Models;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.Notification.Worker.data
{
    public sealed class NotificationsDbContext : DbContext
    {
        public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
        {
        }
        public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var processedMessage =
                modelBuilder.Entity<ProcessedMessage>();

            processedMessage.HasKey(x => x.EventId);

            processedMessage.Property(x => x.EventType)
                .HasMaxLength(200)
                .IsRequired();

            processedMessage.HasIndex(x => x.ProcessedAtUtc);
        }
    }
}
