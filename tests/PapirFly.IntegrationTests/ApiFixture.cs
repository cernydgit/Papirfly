using System.Text.Json;
using Alba;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PapirFly.Infrastructure.Persistence;

namespace PapirFly.IntegrationTests;

public sealed class ApiFixture : IAsyncLifetime
{
    public IAlbaHost Host { get; private set; } = null!;

    public async Task InitializeAsync() => Host = await AlbaHost.For<Program>(builder =>
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
    });

    public async Task ResetAsync()
    {
        var factory = Host.Services.GetRequiredService<IDbContextFactory<ArticlesDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
    }

    public async Task DisposeAsync() => await Host.DisposeAsync();

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

public sealed record ApiResponse(int Status, JsonElement Json);
