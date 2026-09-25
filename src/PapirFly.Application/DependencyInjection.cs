using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using PapirFly.Application.Articles;
using PapirFly.Application.Articles.Commands;
using PapirFly.Domain.Articles;

namespace PapirFly.Application;

/// <summary>Registers application use cases and mapping configuration.</summary>
public static class DependencyInjection
{
    /// <summary>Registers MediatR with the application handlers and a compiled, host-specific Mapster configuration.</summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configureMappings">Optional mappings supplied by the composition root for its boundary contracts.</param>
    /// <returns>The same collection for chained registrations.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, Action<TypeAdapterConfig>? configureMappings = null)
    {
        var mapping = new TypeAdapterConfig();
        mapping.NewConfig<ArticleValues, Article>()
            .Ignore(destination => destination.ArticleId)
            .Ignore(destination => destination.Version)
            .Map(destination => destination.Currency,
                source => string.IsNullOrWhiteSpace(source.Currency) ? null : source.Currency);
        mapping.NewConfig<CreateArticleCommand, Article>().Inherits<ArticleValues, Article>();
        mapping.NewConfig<UpdateArticleCommand, Article>().Inherits<ArticleValues, Article>();
        configureMappings?.Invoke(mapping);
        mapping.Compile();

        services.AddSingleton(mapping);
        services.AddSingleton<IMapper>(new Mapper(mapping));
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining<CreateArticleHandler>());
        return services;
    }
}
