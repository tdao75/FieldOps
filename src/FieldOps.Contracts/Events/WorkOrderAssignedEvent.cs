using System;
using System.Collections.Generic;
using System.Text;

namespace FieldOps.Contracts.Events
{
    public sealed record WorkOrderAssignedEvent
    {
        public Guid EventId { get; init; }

        public DateTime OccurredAtUtc { get; init; }

        public Guid WorkOrderId { get; init; }

        public Guid TechnicianId { get; init; }

        public required string Title { get; init; }

        public required string Location { get; init; }
        public string TechnicianName { get; init; } = string.Empty;

        public string TechnicianEmail { get; init; } = string.Empty;
    }
}
