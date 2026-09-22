using FieldOps.WorkOrders.Api.Enums;

namespace FieldOps.WorkOrders.Api.Models
{
    public class WorkOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Title { get; set; }

        public string? Description { get; set; }

        public required string Location { get; set; }

        public WorkOrderPriority Priority { get; set; }

        public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Submitted;

        public Guid? AssignedTechnicianId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
