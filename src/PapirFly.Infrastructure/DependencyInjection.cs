using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using PapirFly.Application.Articles;
using PapirFly.Infrastructure.Persistence;

namespace PapirFly.Infrastructure;

public static class DependencyInjection
{
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
