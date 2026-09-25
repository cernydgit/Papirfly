using Mapster;
using PapirFly.Api.DTOs;
using PapirFly.Application.Articles.Commands;
using PapirFly.Application.Articles.Queries;
using PapirFly.Domain.Articles;

namespace PapirFly.Api;

/// <summary>Defines mappings at the boundary between HTTP contracts and application requests.</summary>
public static class ArticleMappings
{
    /// <summary>Adds API mappings to the host's mapping configuration before it is compiled.</summary>
    /// <param name="mapping">The configuration shared by this host's Mapster mapper.</param>
    public static void Configure(TypeAdapterConfig mapping)
    {
        mapping.NewConfig<CreateArticleRequest, CreateArticleCommand>();
        mapping.NewConfig<UpdateArticleRequest, UpdateArticleCommand>()
            .Ignore(destination => destination.ArticleId);
        mapping.NewConfig<FindArticlesRequest, FindArticlesQuery>();
        mapping.NewConfig<Article, ArticleResponse>();
    }
}
