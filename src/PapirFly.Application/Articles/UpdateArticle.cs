using MapsterMapper;

namespace PapirFly.Application.Articles;

public sealed record UpdateArticleCommand : ArticleInput
{
    public Guid? Version { get; init; }
}

public sealed class UpdateArticleHandler(IArticleRepository repository, IMapper mapper)
{
    public async Task<ArticleDto> Handle(int articleId, UpdateArticleCommand command, CancellationToken cancellationToken)
    {
        ArticleValidator.EnsureValid(command);
        var article = await repository.GetAsync(articleId, cancellationToken)
            ?? throw new ArticleNotFoundException(articleId);
        var expectedVersion = command.Version!.Value;
        if (article.Version != expectedVersion)
            throw new ArticleConflictException(articleId);

        mapper.Map(command, article);
        await repository.UpdateAsync(article, expectedVersion, cancellationToken);
        return mapper.Map<ArticleDto>(article);
    }
}
