using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using DuonDevKit.AspNetCore.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace DuonDevKit.AspNetCore.Tests.Validation
{
    public class ValidationFilterTests
    {
        private class CreateOrderRequest
        {
            [Required, MaxLength(50)]
            public string? CustomerName { get; set; }

            [Range(1, 100)]
            public int Quantity { get; set; }
        }

        private static async Task<HttpResponseMessage> PostAsync(CreateOrderRequest body)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var app = builder.Build();

            app.MapPost("/orders", (CreateOrderRequest request) => Results.Ok("created"))
                .WithDuonDevKitValidation<CreateOrderRequest>();

            await app.StartAsync();

            var client = app.GetTestServer().CreateClient();
            return await client.PostAsJsonAsync("/orders", body);
        }

        [Fact]
        public async Task WithDuonDevKitValidation_ValidRequest_PassesThroughToHandler()
        {
            var response = await PostAsync(new CreateOrderRequest { CustomerName = "Alice", Quantity = 2 });

            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal("\"created\"", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task WithDuonDevKitValidation_InvalidRequest_ShortCircuitsWith400ValidationProblem()
        {
            var response = await PostAsync(new CreateOrderRequest { CustomerName = null, Quantity = 0 });

            Assert.Equal(400, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);

            Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Validation", json.RootElement.GetProperty("title").GetString());
            Assert.Equal(ErrorCodes.ValidationFailed, json.RootElement.GetProperty("errorCode").GetString());

            var errors = json.RootElement.GetProperty("errors");
            Assert.True(errors.TryGetProperty(nameof(CreateOrderRequest.CustomerName), out _));
            Assert.True(errors.TryGetProperty(nameof(CreateOrderRequest.Quantity), out _));
        }

        [Fact]
        public async Task WithDuonDevKitValidation_PartiallyInvalidRequest_OnlyReportsTheViolatedField()
        {
            var response = await PostAsync(new CreateOrderRequest { CustomerName = "Bob", Quantity = 0 });

            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            var errors = json.RootElement.GetProperty("errors");

            Assert.True(errors.TryGetProperty(nameof(CreateOrderRequest.Quantity), out _));
            Assert.False(errors.TryGetProperty(nameof(CreateOrderRequest.CustomerName), out _));
        }
    }
}
