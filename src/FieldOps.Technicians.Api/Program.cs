using FieldOps.Technicians.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TechniciansDbContext>(
    options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString(
                "TechniciansDatabase")));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Keep local HTTP testing simple.
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();