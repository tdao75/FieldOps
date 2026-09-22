using System.ComponentModel.DataAnnotations;
using FieldOps.WorkOrders.Api.Enums;
namespace FieldOps.WorkOrders.Api.DTOs
{
    public class CreateWorkOrderRequest
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(250)]
        public string Location { get; set; } = string.Empty;

        [EnumDataType(typeof(WorkOrderPriority))]
        public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    }
}
