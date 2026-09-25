using MapsterMapper;
using PapirFly.Application.Articles.Queries;
using PapirFly.Application.DTOs;
using PapirFly.Application.Interfaces;

namespace PapirFly.Application.Articles;

/// <summary>Finds articles without modifying stored state.</summary>
/// <param name="repository">The article persistence boundary.</param>
/// <param name="mapper">The configured DTO/entity mapper.</param>
public sealed class FindArticlesHandler(IArticleRepository repository, IMapper mapper)
{
    /// <summary>Reads all articles matching the supplied filters.</summary>
    /// <param name="query">The optional name and category filters, combined with AND.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    /// <returns>Matching articles ordered by identifier, or an empty array.</returns>
    public async Task<ArticleResponse[]> Handle(FindArticlesQuery query, CancellationToken cancellationToken)
    {
        var articles = await repository.FindAsync(query.Name, query.Category, cancellationToken);
        return mapper.Map<ArticleResponse[]>(articles);
    }
}
