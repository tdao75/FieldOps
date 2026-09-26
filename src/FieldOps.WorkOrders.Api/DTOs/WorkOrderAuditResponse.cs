using FieldOps.WorkOrders.Api.Enums;
namespace FieldOps.WorkOrders.Api.DTOs
{
    public sealed class WorkOrderAuditResponse
    {
        public Guid Id { get; init; }

        public Guid WorkOrderId { get; init; }

        public WorkOrderAuditAction Action { get; init; }

        public WorkOrderStatus? PreviousStatus { get; init; }

        public WorkOrderStatus? NewStatus { get; init; }

        public Guid? PreviousTechnicianId { get; init; }

        public Guid? NewTechnicianId { get; init; }

        public string? ChangedByUserId { get; init; }

        public string? ChangedByEmail { get; init; }

        public DateTime OccurredAtUtc { get; init; }
    }
}
