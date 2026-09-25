using System.Text.Json;
using Alba;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PapirFly.Infrastructure.Persistence;

namespace PapirFly.IntegrationTests;

/// <summary>Hosts the real API through Alba and provides isolated persistence for integration scenarios.</summary>
public sealed class ApiFixture : IAsyncLifetime
{
    /// <summary>Gets the application host shared by one test class.</summary>
    public IAlbaHost Host { get; private set; } = null!;

    /// <summary>Starts the API in the Testing environment.</summary>
    /// <returns>A task that completes when the host is ready.</returns>
    public async Task InitializeAsync() => Host = await AlbaHost.For<Program>(builder =>
    {
        builder.UseEnvironment("Testing");
    });

    /// <summary>Clears the host's article store before the next scenario.</summary>
    /// <returns>A task that completes when all stored articles have been removed.</returns>
    public async Task ResetAsync()
    {
        var factory = Host.Services.GetRequiredService<IDbContextFactory<ArticlesDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>Disposes the application host and its services.</summary>
    /// <returns>A task that completes when the host has shut down.</returns>
    public async Task DisposeAsync() => await Host.DisposeAsync();

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
}

/// <summary>Contains the HTTP status and JSON body returned by an integration scenario.</summary>
/// <param name="Status">The actual HTTP status code.</param>
/// <param name="Json">The response JSON, independent of the source document lifetime.</param>
public sealed record ApiResponse(int Status, JsonElement Json);
