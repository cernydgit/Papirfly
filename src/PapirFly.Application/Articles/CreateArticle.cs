using MapsterMapper;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

public sealed record CreateArticleCommand : ArticleInput;

public sealed class CreateArticleHandler(IArticleRepository repository, IMapper mapper)
{
    public async Task<ArticleDto> Handle(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        ArticleValidator.EnsureValid(command);
        var article = mapper.Map<Article>(command);
        await repository.AddAsync(article, cancellationToken);
        return mapper.Map<ArticleDto>(article);
    }
}
