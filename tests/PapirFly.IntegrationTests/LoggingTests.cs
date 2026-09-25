using System.Collections.Concurrent;
using Alba;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace PapirFly.IntegrationTests;

/// <summary>Verifies Serilog integration with application logging and HTTP request completion.</summary>
public sealed class LoggingTests
{
    /// <summary>Checks that application events and successful or failed requests reach the configured Serilog sink.</summary>
    /// <returns>A task that completes after the structured log assertions.</returns>
    [Fact]
    public async Task Serilog_captures_application_events_and_final_http_statuses()
    {
        var sink = new CapturingSink();
        await using var host = await AlbaHost.For<Program>(builder =>
            builder.ConfigureServices(services => services.AddSingleton<ILogEventSink>(sink)));

        var logger = host.Services.GetRequiredService<ILogger<LoggingTests>>();
        logger.LogInformation("Article logging probe {ArticleId}", 42);

        await host.Scenario(s =>
        {
            s.Get.Url("/api/articles");
            s.StatusCodeShouldBeOk();
        });
        await host.Scenario(s =>
        {
            s.Get.Url("/api/articles/999");
            s.StatusCodeShouldBe(404);
        });

        var applicationEvent = Assert.Single(sink.Events, e => e.MessageTemplate.Text == "Article logging probe {ArticleId}");
        Assert.Equal(42, Assert.IsType<ScalarValue>(applicationEvent.Properties["ArticleId"]).Value);
        Assert.Equal("PapirFly.Api", Assert.IsType<ScalarValue>(applicationEvent.Properties["Application"]).Value);

        var requests = sink.Events.Where(e => e.Properties.ContainsKey("RequestMethod")).ToArray();
        Assert.Equal(2, requests.Length);
        Assert.Equal(new[] { 200, 404 }, requests.Select(e => (int)((ScalarValue)e.Properties["StatusCode"]).Value!).Order());
        Assert.All(requests, e => Assert.True(e.Properties.ContainsKey("Elapsed")));
    }

    private sealed class CapturingSink : ILogEventSink
    {
        internal ConcurrentQueue<LogEvent> Events { get; } = new();

        /// <inheritdoc />
        public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
    }
}
