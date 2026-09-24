using FieldOps.WorkOrders.Api.Data;
using FieldOps.WorkOrders.Api.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FieldOps.WorkOrders.Api.Tests
{
    public sealed class WorkOrdersApiFactory : WebApplicationFactory<Program>
    {
        public const string JwtKey = "FieldOps-integration-test-secret-key-2026";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
           builder.UseEnvironment("Testing");
           builder.ConfigureAppConfiguration(
               (_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Jwt:Key"] = JwtKey,
                            ["Jwt:Issuer"] = "FieldOps.Identity.Api",
                            ["Jwt:Audience"] ="FieldOps",
                            ["ConnectionStrings:WorkOrdersDatabase"] = "Host=unused",
                            ["ServiceUrls:TechniciansApi"] = "http://localhost:5072"
                        });
                });

            builder.ConfigureTestServices(services =>
            {
                // Prevent RabbitMQ/outbox processing in tests.
                services.RemoveAll<IHostedService>();
                services.RemoveAll<RabbitMqEventPublisher>();

                services.RemoveAll<DbContextOptions<WorkOrdersDbContext>>();

                services.RemoveAll<WorkOrdersDbContext>();

                services.RemoveAll<IDbContextOptionsConfiguration<WorkOrdersDbContext>>();

                services.AddDbContext<WorkOrdersDbContext>(
                    options =>
                    {
                        options.UseInMemoryDatabase($"WorkOrdersTests-{Guid.NewGuid()}");
                    });

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;

                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;

                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                }
                ).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }); });
        }
    }
}
