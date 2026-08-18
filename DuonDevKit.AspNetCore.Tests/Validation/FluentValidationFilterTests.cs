using System.Net.Http.Json;
using System.Text.Json;
using DuonDevKit.AspNetCore.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.AspNetCore.Tests.Validation
{
    public class FluentValidationFilterTests
    {
        private class CreateOrderRequest
        {
            public string? CustomerName { get; set; }
            public int Quantity { get; set; }
        }

        private class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
        {
            public CreateOrderRequestValidator()
            {
                RuleFor(r => r.CustomerName).NotEmpty().MaximumLength(50);
                RuleFor(r => r.Quantity).InclusiveBetween(1, 100);
            }
        }

        private static async Task<HttpResponseMessage> PostAsync(CreateOrderRequest body)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
            var app = builder.Build();

            app.MapPost("/orders", (CreateOrderRequest request) => Results.Ok("created"))
                .WithDuonDevKitFluentValidation<CreateOrderRequest>();

            await app.StartAsync();

            var client = app.GetTestServer().CreateClient();
            return await client.PostAsJsonAsync("/orders", body);
        }

        [Fact]
        public async Task WithDuonDevKitFluentValidation_ValidRequest_PassesThroughToHandler()
        {
            var response = await PostAsync(new CreateOrderRequest { CustomerName = "Alice", Quantity = 2 });

            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal("\"created\"", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task WithDuonDevKitFluentValidation_InvalidRequest_ShortCircuitsWith400ValidationProblem()
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
        public async Task WithDuonDevKitFluentValidation_PartiallyInvalidRequest_OnlyReportsTheViolatedField()
        {
            var response = await PostAsync(new CreateOrderRequest { CustomerName = "Bob", Quantity = 0 });

            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            var errors = json.RootElement.GetProperty("errors");

            Assert.True(errors.TryGetProperty(nameof(CreateOrderRequest.Quantity), out _));
            Assert.False(errors.TryGetProperty(nameof(CreateOrderRequest.CustomerName), out _));
        }

        [Fact]
        public async Task WithDuonDevKitFluentValidation_NoValidatorRegisteredInDi_ThrowsInsteadOfSkippingValidation()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            // Deliberately no IValidator<CreateOrderRequest> registration.
            var app = builder.Build();

            app.MapPost("/orders", (CreateOrderRequest request) => Results.Ok("created"))
                .WithDuonDevKitFluentValidation<CreateOrderRequest>();

            await app.StartAsync();
            var client = app.GetTestServer().CreateClient();

            // No exception-handling middleware is registered in this minimal test app, so TestServer
            // surfaces the filter's GetRequiredService failure as a real exception on the client call
            // rather than converting it to a response — proving the missing registration is never
            // silently swallowed into "validation passed."
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => client.PostAsJsonAsync("/orders", new CreateOrderRequest { CustomerName = "Alice", Quantity = 2 }));
        }
    }
}
