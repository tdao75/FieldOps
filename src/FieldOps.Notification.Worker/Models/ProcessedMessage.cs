using System;
using System.Collections.Generic;
using System.Text;

namespace FieldOps.Notification.Worker.Models
{
    public sealed class ProcessedMessage
    {
        public Guid EventId { get; set; }

        public DateTime ProcessedAtUtc { get; set; }

        public required string EventType { get; set; }
    }
}
