var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(
        builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactClient",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:5174")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


builder.Services.AddHealthChecks();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance =context.HttpContext.Request.Path;

        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseStatusCodePages();

app.UseCors("ReactClient");

app.MapGet("/", () => Results.Ok(new
{
    service = "FieldOps API Gateway",
    status = "Running"
}));

app.MapReverseProxy();
app.MapHealthChecks("/health");
app.Run();