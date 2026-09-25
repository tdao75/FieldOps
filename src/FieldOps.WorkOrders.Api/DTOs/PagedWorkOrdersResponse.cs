namespace FieldOps.WorkOrders.Api.DTOs
{
    public sealed class PagedWorkOrdersResponse
    {
        public required IReadOnlyList<WorkOrderResponse> Items { get; init; }

        public int PageNumber { get; init; }

        public int PageSize { get; init; }

        public int TotalCount { get; init; }

        public int TotalPages { get; init; }

        public int OpenCount { get; init; }

        public int EmergencyCount { get; init; }
    }
}
