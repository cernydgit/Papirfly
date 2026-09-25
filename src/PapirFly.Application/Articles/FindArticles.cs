using MapsterMapper;

namespace PapirFly.Application.Articles;

public sealed record FindArticlesQuery(string? Name = null, string? Category = null);

public sealed class FindArticlesHandler(IArticleRepository repository, IMapper mapper)
{
    public async Task<ArticleDto[]> Handle(FindArticlesQuery query, CancellationToken cancellationToken)
    {
        var articles = await repository.FindAsync(query.Name, query.Category, cancellationToken);
        return mapper.Map<ArticleDto[]>(articles);
    }
}
