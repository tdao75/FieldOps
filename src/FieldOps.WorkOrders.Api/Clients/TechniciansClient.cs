using System.Net;

namespace FieldOps.WorkOrders.Api.Clients
{
    public sealed class TechniciansClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TechniciansClient> _logger;

        public TechniciansClient(
            HttpClient httpClient,
            ILogger<TechniciansClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<TechnicianDetails?> GetByIdAsync(
            Guid technicianId,
            CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(
                $"api/technicians/{technicianId}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var technician =
                await response.Content.ReadFromJsonAsync<
                    TechnicianDetails>(
                        cancellationToken);

            if (technician is null)
            {
                throw new InvalidOperationException(
                    "Technicians API returned an empty response.");
            }

            _logger.LogInformation(
                "Technician {TechnicianId} retrieved from Technicians API.",
                technicianId);

            return technician;
        }
    }
}
