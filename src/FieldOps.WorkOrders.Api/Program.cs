using FieldOps.WorkOrders.Api.Data;
using Microsoft.EntityFrameworkCore;
using FieldOps.WorkOrders.Api.Messaging;
using FieldOps.WorkOrders.Api.Clients;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{

    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
}); 
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
// Add Entity Framework
builder.Services.AddDbContext<WorkOrdersDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("WorkOrdersDatabase")));

builder.Services.AddSingleton<RabbitMqEventPublisher>();

builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddHttpClient<TechniciansClient>(
    client =>
    {
        var baseUrl =
            builder.Configuration["ServiceUrls:TechniciansApi"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("ServiceUrls:TechniciansApi is missing.");
        }

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(5);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
