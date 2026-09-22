using FieldOps.WorkOrders.Api.Enums;

namespace FieldOps.WorkOrders.Api.DTOs
{
    public class WorkOrderResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Location { get; set; } = string.Empty;

        public WorkOrderPriority Priority { get; set; }

        public WorkOrderStatus Status { get; set; }

        public Guid? AssignedTechnicianId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
