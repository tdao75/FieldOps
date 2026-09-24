using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace FieldOps.WorkOrders.Api.Tests
{
    public sealed class WorkOrdersAuthorizationTests :IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public WorkOrdersAuthorizationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect=false });
        }

        [Fact]
        public async Task Create_WithoutToken_ReturnsUnathorized()
        {
            var request = new
            {
                title = "Unauthorized test",
            description = "This must not be created.",
            location = "Test building",
            priority = "High"
            };

            var response = await _client.PostAsJsonAsync("api/workorders", request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Create_WithoutRequiredRole_ReturnsForbidden()
        {
            var token = CreateToken();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

        private static string CreateToken(params string[] roles)
        {
            const string key = "FieldOps-development-secret-key-change-before-production-2026";

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),

                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),

                new(JwtRegisteredClaimNames.Email, "test-user@fieldops.com")
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "FieldOps.Identity.Api",
                audience: "FieldOps",
                claims:claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials:credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task Create_WithDispatcherRole_PassesAuthorization()
        {
            var token = CreateToken("Dispatcher");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new StringContent("{}", Encoding.UTF8,"application/json");

            var response = await _client.PostAsync("/api/workorders", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
