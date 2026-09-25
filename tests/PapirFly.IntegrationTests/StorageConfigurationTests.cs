using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PapirFly.Infrastructure;
using PapirFly.Infrastructure.Persistence;

namespace PapirFly.IntegrationTests;

/// <summary>Verifies environment defaults and the application's storage registration without opening database connections.</summary>
public sealed class StorageConfigurationTests
{
    /// <summary>Verifies that each committed environment configuration selects InMemory and accepts JSON comments.</summary>
    /// <param name="environment">The environment configuration file to load.</param>
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void Every_environment_defaults_to_in_memory(string environment)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json")
            .Build();
        using var services = CreateServices(configuration);
        using var context = services.GetRequiredService<IDbContextFactory<ArticlesDbContext>>().CreateDbContext();
        Assert.True(context.Database.IsInMemory());
    }

    /// <summary>Verifies that configuration selects Npgsql through the normal application registration.</summary>
    [Fact]
    public void Explicit_postgresql_configuration_selects_npgsql()
    {
        using var services = CreateServices(PostgreSqlConfiguration("Host=localhost;Database=registration_test"));
        using var context = services.GetRequiredService<IDbContextFactory<ArticlesDbContext>>().CreateDbContext();
        Assert.True(context.Database.IsNpgsql());
    }

    /// <summary>Verifies PostgreSQL cannot accidentally start without a connection string.</summary>
    [Fact]
    public void PostgreSql_requires_a_connection_string()
    {
        using var services = CreateServices(PostgreSqlConfiguration(null));
        Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IDbContextFactory<ArticlesDbContext>>());
    }

    /// <summary>Verifies an unknown provider is rejected instead of silently falling back to InMemory.</summary>
    [Fact]
    public void Unknown_provider_is_rejected()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "UnsupportedDatabase"
        }).Build();
        using var services = CreateServices(configuration);
        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<IDbContextFactory<ArticlesDbContext>>());
    }

    private static ServiceProvider CreateServices(IConfiguration configuration) =>
        new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();

    private static IConfiguration PostgreSqlConfiguration(string? connectionString) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "PostgreSql",
            ["Storage:ConnectionString"] = connectionString
        }).Build();
}
