using MediatR;
using PapirFly.Application.Articles.Queries;
using PapirFly.Application.Interfaces;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

/// <summary>Retrieves a single article without modifying stored state.</summary>
/// <param name="repository">The article persistence boundary.</param>
public sealed class GetArticleHandler(IArticleRepository repository)
    : IRequestHandler<GetArticleQuery, Article>
{
    /// <summary>Reads an article by its identifier.</summary>
    /// <param name="query">The identifier to look up.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The article's current values and concurrency token.</returns>
    /// <exception cref="ArticleNotFoundException">The article does not exist.</exception>
    public async Task<Article> Handle(GetArticleQuery query, CancellationToken cancellationToken)
    {
        return await repository.GetAsync(query.ArticleId, cancellationToken)
            ?? throw new ArticleNotFoundException(query.ArticleId);
    }
}
