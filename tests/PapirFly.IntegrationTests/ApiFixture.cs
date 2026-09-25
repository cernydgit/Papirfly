using System.Collections.Concurrent;
using System.Text.Json;
using Alba;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PapirFly.Infrastructure.Configuration;
using PapirFly.Infrastructure.Persistence;
using Serilog.Core;
using Serilog.Events;
using Testcontainers.PostgreSql;

namespace PapirFly.IntegrationTests;

/// <summary>Hosts the real API through Alba and provides isolated persistence for integration scenarios.</summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? postgres;

    /// <summary>Gets the application host shared by one test class.</summary>
    public IAlbaHost Host { get; private set; } = null!;

    /// <summary>Gets the provider selected by Testing configuration and PAPIRFLY_TEST_ overrides.</summary>
    public StorageProvider Provider { get; private set; }

    /// <summary>Gets structured events emitted by this fixture's application hosts.</summary>
    public ConcurrentQueue<LogEvent> Logs { get; } = new();

    /// <summary>Starts the API, using a disposable PostgreSQL container when configured.</summary>
    /// <returns>A task that completes when the container, host and schema are ready.</returns>
    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Testing.json", optional: false)
            .AddEnvironmentVariables("PAPIRFLY_TEST_")
            .Build();
        Provider = configuration.GetValue<StorageProvider>("Storage:Provider");
        if (!Enum.IsDefined(Provider))
            throw new InvalidOperationException("Testing Storage:Provider must be InMemory or PostgreSql.");

        try
        {
            if (Provider == StorageProvider.PostgreSql)
            {
                postgres = new PostgreSqlBuilder(configuration["Testcontainers:PostgresImage"] ?? "postgres:17-alpine")
                    .WithDatabase("papirfly_tests")
                    .WithUsername("papirfly_test")
                    .WithPassword(Guid.NewGuid().ToString("N"))
                    .Build();
                await postgres.StartAsync();
            }

            Host = await StartHostAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    /// <summary>Clears the host's article store before the next scenario.</summary>
    /// <returns>A task that completes when all stored articles have been removed.</returns>
    public async Task ResetAsync()
    {
        var factory = Host.Services.GetRequiredService<IDbContextFactory<ArticlesDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        if (context.Database.IsRelational())
            await context.Articles.ExecuteDeleteAsync();
        else
            await context.Database.EnsureDeletedAsync();
    }

    /// <summary>Disposes the application host and then removes its test container, if any.</summary>
    /// <returns>A task that completes after all fixture resources have been released.</returns>
    public async Task DisposeAsync()
    {
        try
        {
            if (Host is not null)
                await Host.DisposeAsync();
        }
        finally
        {
            if (postgres is not null)
                await postgres.DisposeAsync();
        }
    }

    /// <summary>Restarts the API while retaining the PostgreSQL container to verify database durability.</summary>
    /// <returns>A task that completes when the replacement host is ready.</returns>
    public async Task RestartAsync()
    {
        await Host.DisposeAsync();
        Host = await StartHostAsync();
    }

    /// <summary>Sends a raw JSON request through Alba and reads its JSON response.</summary>
    /// <param name="method">GET, POST or PUT.</param>
    /// <param name="url">The relative request URL.</param>
    /// <param name="json">The raw body for POST or PUT.</param>
    /// <param name="expectedStatus">The expected status, or null when the caller will assert the status.</param>
    /// <returns>The actual status and a detached JSON response value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The supplied HTTP method is not supported by this helper.</exception>
    public async Task<ApiResponse> Send(string method, string url, string? json = null, int? expectedStatus = 200)
    {
        var result = await Host.Scenario(scenario =>
        {
            switch (method)
            {
                case "GET": scenario.Get.Url(url); break;
                case "POST": scenario.Post.RawJson(json!).ToUrl(url); break;
                case "PUT": scenario.Put.RawJson(json!).ToUrl(url); break;
                default: throw new ArgumentOutOfRangeException(nameof(method));
            }

            if (expectedStatus is { } status)
                scenario.StatusCodeShouldBe(status);
            else
                scenario.IgnoreStatusCode();
        });

        using var document = JsonDocument.Parse(await result.ReadAsTextAsync());
        return new(result.Context.Response.StatusCode, document.RootElement.Clone());
    }

    private Task<IAlbaHost> StartHostAsync() => AlbaHost.For<Program>(builder =>
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Replace the application's storage options through DI before its context factory is resolved.
            // Never use a configured development/production connection for test writes or cleanup.
            services.PostConfigure<StorageOptions>(storage =>
            {
                storage.Provider = Provider;
                storage.ConnectionString = postgres?.GetConnectionString();
            });
            services.AddSingleton<ILogEventSink>(new CapturingSink(Logs));
        });
    });

    private sealed class CapturingSink(ConcurrentQueue<LogEvent> events) : ILogEventSink
    {
        /// <inheritdoc />
        public void Emit(LogEvent logEvent) => events.Enqueue(logEvent);
    }
}

/// <summary>Contains the HTTP status and JSON body returned by an integration scenario.</summary>
/// <param name="Status">The actual HTTP status code.</param>
/// <param name="Json">The response JSON, independent of the source document lifetime.</param>
public sealed record ApiResponse(int Status, JsonElement Json);
