using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using PapirFly.Application.Interfaces;
using PapirFly.Infrastructure.Persistence;

namespace PapirFly.Infrastructure;

/// <summary>Registers the EF Core article persistence implementation.</summary>
public static class DependencyInjection
{
    /// <summary>Registers an isolated InMemory store, a context factory and the article repository.</summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same collection for chained registrations.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // One store per application host, shared by every short-lived context of that host.
        var databaseRoot = new InMemoryDatabaseRoot();
        services.AddDbContextFactory<ArticlesDbContext>(options =>
            options.UseInMemoryDatabase("Articles", databaseRoot));
        services.AddScoped<IArticleRepository, ArticleRepository>();
        return services;
    }
}
