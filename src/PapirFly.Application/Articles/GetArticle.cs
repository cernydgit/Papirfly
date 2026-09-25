using MapsterMapper;
using MediatR;
using PapirFly.Application.Articles.Queries;
using PapirFly.Application.DTOs;
using PapirFly.Application.Interfaces;

namespace PapirFly.Application.Articles;

/// <summary>Retrieves a single article without modifying stored state.</summary>
/// <param name="repository">The article persistence boundary.</param>
/// <param name="mapper">The configured DTO/entity mapper.</param>
public sealed class GetArticleHandler(IArticleRepository repository, IMapper mapper)
    : IRequestHandler<GetArticleQuery, ArticleResponse>
{
    /// <summary>Reads an article by its identifier.</summary>
    /// <param name="query">The identifier to look up.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The article's current values and concurrency token.</returns>
    /// <exception cref="ArticleNotFoundException">The article does not exist.</exception>
    public async Task<ArticleResponse> Handle(GetArticleQuery query, CancellationToken cancellationToken)
    {
        var article = await repository.GetAsync(query.ArticleId, cancellationToken)
            ?? throw new ArticleNotFoundException(query.ArticleId);
        return mapper.Map<ArticleResponse>(article);
    }
}
