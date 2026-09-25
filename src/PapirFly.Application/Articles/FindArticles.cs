using MediatR;
using PapirFly.Application.Articles.Queries;
using PapirFly.Application.Interfaces;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

/// <summary>Finds articles without modifying stored state.</summary>
/// <param name="repository">The article persistence boundary.</param>
public sealed class FindArticlesHandler(IArticleRepository repository)
    : IRequestHandler<FindArticlesQuery, IReadOnlyList<Article>>
{
    /// <summary>Reads all articles matching the supplied filters.</summary>
    /// <param name="query">The optional name and category filters, combined with AND.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    /// <returns>Matching articles ordered by identifier, or an empty array.</returns>
    public Task<IReadOnlyList<Article>> Handle(FindArticlesQuery query, CancellationToken cancellationToken) =>
        repository.FindAsync(query.Name, query.Category, cancellationToken);
}
