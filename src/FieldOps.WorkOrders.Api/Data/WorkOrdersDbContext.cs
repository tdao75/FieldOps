using FieldOps.WorkOrders.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace FieldOps.WorkOrders.Api.Data
{
    public class WorkOrdersDbContext : DbContext
    {
        public WorkOrdersDbContext(DbContextOptions<WorkOrdersDbContext> options) : base(options)
        {
        }
        public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
        public DbSet<OutboxMessage> OutboxMessages =>Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var workOrder = modelBuilder.Entity<WorkOrder>();

            workOrder.HasKey(x  => x.Id);
            workOrder.Property(x => x.Title).IsRequired().HasMaxLength(150);
            workOrder.Property(x => x.Description).HasMaxLength(2000);
            workOrder.Property(x => x.Location).HasMaxLength(250).IsRequired();
            workOrder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(30);
            workOrder.Property(x=> x.Status).HasConversion<string>().HasMaxLength(30);
            workOrder.HasIndex(x => x.Status);
            workOrder.HasIndex(x => x.AssignedTechnicianId);
            workOrder.HasIndex(x => x.CreatedAtUtc);
            var outboxMessage = modelBuilder.Entity<OutboxMessage>();

            outboxMessage.HasKey(x => x.Id);

            outboxMessage.Property(x => x.EventType).HasMaxLength(200).IsRequired();

            outboxMessage.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();

            outboxMessage.Property(x => x.LastError).HasMaxLength(2000);

            outboxMessage.HasIndex(x => new
            {
                x.ProcessedAtUtc,
                x.OccurredAtUtc
            });
        }
    }
}
