using FieldOps.WorkOrders.Api.Data;
using FieldOps.WorkOrders.Api.DTOs;
using FieldOps.WorkOrders.Api.Enums;
using FieldOps.WorkOrders.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldOps.WorkOrders.Api.Tests;

public sealed class WorkOrdersWorkflowTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly WorkOrdersApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions =CreateJsonOptions();

    public WorkOrdersWorkflowTests(WorkOrdersApiFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "Dispatcher");

        ResetDatabase();
    }

    [Fact]
    public async Task UpdateStatus_AssignedToInProgress_ReturnsOk()
    {
        var workOrder = CreateWorkOrder(
            WorkOrderStatus.Assigned);

        await SeedAsync(workOrder);

        var response = await _client.PutAsJsonAsync(
            $"/api/workorders/{workOrder.Id}/status",
            new
            {
                status = "InProgress"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<WorkOrdersDbContext>();

        var savedWorkOrder =
            await dbContext.WorkOrders.FindAsync(
                workOrder.Id);

        Assert.NotNull(savedWorkOrder);
        Assert.Equal(
            WorkOrderStatus.InProgress,
            savedWorkOrder.Status);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsConflict()
    {
        var workOrder = CreateWorkOrder(
            WorkOrderStatus.Submitted);

        await SeedAsync(workOrder);

        var response = await _client.PutAsJsonAsync(
            $"/api/workorders/{workOrder.Id}/status",
            new
            {
                status = "Completed"
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_CompletedWorkOrder_ReturnsConflict()
    {
        var workOrder = CreateWorkOrder(
            WorkOrderStatus.Completed);

        await SeedAsync(workOrder);

        var response = await _client.PutAsJsonAsync(
            $"/api/workorders/{workOrder.Id}/status",
            new
            {
                status = "InProgress"
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsRequestedPage()
    {
        var workOrders = Enumerable.Range(1, 15)
            .Select(index => new WorkOrder
            {
                Title = $"Work order {index}",
                Description = $"Description {index}",
                Location = $"Building {index}",
                Priority = WorkOrderPriority.Medium,
                Status = WorkOrderStatus.Submitted,
                CreatedAtUtc =
                    DateTime.UtcNow.AddMinutes(-index)
            })
            .ToArray();

        await SeedAsync(workOrders);

        var response =
            await _client.GetFromJsonAsync<
                PagedWorkOrdersResponse>("/api/workorders?pageNumber=2&pageSize=5", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(5, response.Items.Count);
        Assert.Equal(2, response.PageNumber);
        Assert.Equal(5, response.PageSize);
        Assert.Equal(15, response.TotalCount);
        Assert.Equal(15, response.AllCount);
        Assert.Equal(3, response.TotalPages);
    }

    [Fact]
    public async Task GetAll_FiltersByStatusAndPriority()
    {
        await SeedAsync(
            CreateWorkOrder(
                WorkOrderStatus.Assigned,
                WorkOrderPriority.High,
                "Matching order"),
            CreateWorkOrder(
                WorkOrderStatus.Submitted,
                WorkOrderPriority.High,
                "Wrong status"),
            CreateWorkOrder(
                WorkOrderStatus.Assigned,
                WorkOrderPriority.Low,
                "Wrong priority"));

        var response =
            await _client.GetFromJsonAsync<
                PagedWorkOrdersResponse>(
                "/api/workorders" +
                "?status=Assigned" +
                "&priority=High",JsonOptions);

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal(
            "Matching order",
            response.Items[0].Title);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(3, response.AllCount);
    }

    [Fact]
    public async Task GetAll_SearchesTitleAndLocation()
    {
        await SeedAsync(
            CreateWorkOrder(
                WorkOrderStatus.Submitted,
                WorkOrderPriority.High,
                "Leaking pipe",
                "Building A"),
            CreateWorkOrder(
                WorkOrderStatus.Submitted,
                WorkOrderPriority.Low,
                "Replace light",
                "Warehouse"));

        var response = await _client.GetFromJsonAsync<PagedWorkOrdersResponse>("/api/workorders?search=warehouse",JsonOptions);

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal(
            "Replace light",
            response.Items[0].Title);
    }

    private void ResetDatabase()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<WorkOrdersDbContext>();

        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    private async Task SeedAsync(
        params WorkOrder[] workOrders)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<WorkOrdersDbContext>();

        dbContext.WorkOrders.AddRange(workOrders);
        await dbContext.SaveChangesAsync();
    }

    private static WorkOrder CreateWorkOrder(
        WorkOrderStatus status,
        WorkOrderPriority priority =
            WorkOrderPriority.High,
        string title = "Test work order",
        string location = "Test building")
    {
        return new WorkOrder
        {
            Title = title,
            Description = "Integration test",
            Location = location,
            Priority = priority,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options =
            new JsonSerializerOptions(
                JsonSerializerDefaults.Web);

        options.Converters.Add(
            new JsonStringEnumConverter());

        return options;
    }
}