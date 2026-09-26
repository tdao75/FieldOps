using FieldOps.WorkOrders.Api.Enums;
namespace FieldOps.WorkOrders.Api.Models
{
    public sealed class WorkOrderAuditEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid WorkOrderId { get; set; }

        public WorkOrderAuditAction Action { get; set; }

        public WorkOrderStatus? PreviousStatus { get; set; }

        public WorkOrderStatus? NewStatus { get; set; }

        public Guid? PreviousTechnicianId { get; set; }

        public Guid? NewTechnicianId { get; set; }

        public string? ChangedByUserId { get; set; }

        public string? ChangedByEmail { get; set; }

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }
}
