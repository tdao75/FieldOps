using System.ComponentModel.DataAnnotations;
using FieldOps.WorkOrders.Api.Enums;
namespace FieldOps.WorkOrders.Api.DTOs
{
    public class UpdateWorkOrderStatusRequest
    {
        [EnumDataType(typeof(WorkOrderStatus))]
        public WorkOrderStatus Status { get; set; }
    }
}
