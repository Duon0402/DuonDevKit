using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DuonDevKit.AspNetCore.Tests
{
    public class ApplicationBuilderExtensionsTests
    {
        private static async Task<(HttpResponseMessage Response, TestServer Server)> RunAsync(Action<IApplicationBuilder> configureAfterExceptionHandling)
        {
            var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.Configure(app =>
                    {
                        app.UseDuonDevKitExceptionHandling();
                        configureAfterExceptionHandling(app);
                    });
                })
                .StartAsync();

            var server = host.GetTestServer();
            var response = await server.CreateClient().GetAsync("/");
            return (response, server);
        }

        [Fact]
        public async Task UseDuonDevKitExceptionHandling_UnhandledException_Returns500WithProblemDetailsBody()
        {
            var (response, _) = await RunAsync(app => app.Run(_ => throw new InvalidOperationException("boom")));

            Assert.Equal(500, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Unexpected", json.RootElement.GetProperty("title").GetString());
            Assert.Equal("UNHANDLED_EXCEPTION", json.RootElement.GetProperty("errorCode").GetString());
        }

        [Fact]
        public async Task UseDuonDevKitExceptionHandling_NoException_PassesRequestThrough()
        {
            var (response, _) = await RunAsync(app => app.Run(async ctx => await ctx.Response.WriteAsync("ok")));

            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal("ok", await response.Content.ReadAsStringAsync());
        }
    }
}
