using MapsterMapper;

namespace PapirFly.Application.Articles;

public sealed record GetArticleQuery(int ArticleId);

public sealed class GetArticleHandler(IArticleRepository repository, IMapper mapper)
{
    public async Task<ArticleDto> Handle(GetArticleQuery query, CancellationToken cancellationToken)
    {
        var article = await repository.GetAsync(query.ArticleId, cancellationToken)
            ?? throw new ArticleNotFoundException(query.ArticleId);
        return mapper.Map<ArticleDto>(article);
    }
}
