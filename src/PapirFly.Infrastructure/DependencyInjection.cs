using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PapirFly.Application.Interfaces;
using PapirFly.Infrastructure.Configuration;
using PapirFly.Infrastructure.Persistence;

namespace PapirFly.Infrastructure;

/// <summary>Registers the EF Core article persistence implementation.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the configured storage provider, a context factory and the article repository.</summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The configuration containing the Storage section.</param>
    /// <returns>The same collection for chained registrations.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .Validate(options => Enum.IsDefined(options.Provider), "Storage:Provider must be InMemory or PostgreSql.")
            .Validate(options => options.Provider != StorageProvider.PostgreSql || !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Storage:ConnectionString is required for PostgreSql.")
            .ValidateOnStart();

        services.AddSingleton<InMemoryDatabaseRoot>();
        services.AddDbContextFactory<ArticlesDbContext>((provider, options) =>
        {
            var storage = provider.GetRequiredService<IOptions<StorageOptions>>().Value;
            if (storage.Provider == StorageProvider.PostgreSql)
                options.UseNpgsql(storage.ConnectionString);
            else
                options.UseInMemoryDatabase("Articles", provider.GetRequiredService<InMemoryDatabaseRoot>());
        });
        services.AddScoped<IArticleRepository, ArticleRepository>();
        return services;
    }

    /// <summary>Initializes InMemory storage or applies committed PostgreSQL migrations without deleting data.</summary>
    /// <param name="services">The fully configured application service provider.</param>
    /// <param name="cancellationToken">Cancels database initialization.</param>
    /// <returns>A task that completes when the article schema is ready.</returns>
    public static async Task InitializeStorageAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var factory = services.GetRequiredService<IDbContextFactory<ArticlesDbContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync(cancellationToken);
        else
            await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
