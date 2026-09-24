using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace FieldOps.WorkOrders.Api.Tests;

public sealed class WorkOrdersAuthorizationTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public WorkOrdersAuthorizationTests(WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var request = new
        {
            title = "Unauthorized test",
            description = "This must not be created.",
            location = "Test building",
            priority = "High"
        };

        var response = await _client.PostAsJsonAsync("/api/workorders", request);

        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutRequiredRole_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "NoRole");

        var request = new
        {
            title = "Forbidden test",
            description = "Valid login but insufficient role.",
            location = "Test building",
            priority = "High"
        };

        var response = await _client.PostAsJsonAsync("/api/workorders", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDispatcherRole_PassesAuthorization()
    {
        _client.DefaultRequestHeaders.Authorization =new AuthenticationHeaderValue("Bearer", "Dispatcher");

        using var content = new StringContent("{}",Encoding.UTF8,"application/json");

        var response = await _client.PostAsync("/api/workorders", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}