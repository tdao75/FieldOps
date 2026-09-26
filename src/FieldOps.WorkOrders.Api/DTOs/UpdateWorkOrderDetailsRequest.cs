using FieldOps.WorkOrders.Api.Enums;
using System.ComponentModel.DataAnnotations;
namespace FieldOps.WorkOrders.Api.DTOs
{
    public sealed class UpdateWorkOrderDetailsRequest
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(250)]
        public string Location { get; set; } = string.Empty;

        public WorkOrderPriority Priority { get; set; }
    }
}
