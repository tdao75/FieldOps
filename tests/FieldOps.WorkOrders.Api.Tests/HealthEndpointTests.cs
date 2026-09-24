using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace FieldOps.WorkOrders.Api.Tests
{
    public sealed class HealthEndpointTests:IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public HealthEndpointTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(
           new WebApplicationFactoryClientOptions
           {
               AllowAutoRedirect = false
           });
        }

        [Fact]
        public async Task Health_ReturnOk()
        {
            var response = await _client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", content);
        }
    }
}
