using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using PapirFly.Application.Articles;
using PapirFly.Application.Articles.Commands;
using PapirFly.Application.DTOs;
using PapirFly.Domain.Articles;

namespace PapirFly.Application;

/// <summary>Registers application use cases and mapping configuration.</summary>
public static class DependencyInjection
{
    /// <summary>Registers MediatR with the application handlers and a compiled, host-specific Mapster configuration.</summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same collection for chained registrations.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var mapping = new TypeAdapterConfig();
        mapping.NewConfig<ArticleRequest, Article>()
            .Ignore(destination => destination.ArticleId)
            .Ignore(destination => destination.Version)
            .Map(destination => destination.Currency,
                source => string.IsNullOrWhiteSpace(source.Currency) ? null : source.Currency);
        mapping.NewConfig<CreateArticleCommand, Article>().Inherits<ArticleRequest, Article>();
        mapping.NewConfig<UpdateArticleRequest, Article>().Inherits<ArticleRequest, Article>();
        mapping.NewConfig<Article, ArticleResponse>();
        mapping.Compile();

        services.AddSingleton(mapping);
        services.AddSingleton<IMapper>(new Mapper(mapping));
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining<CreateArticleHandler>());
        return services;
    }
}
