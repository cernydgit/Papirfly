using MapsterMapper;
using MediatR;
using PapirFly.Application.Articles.Commands;
using PapirFly.Application.Interfaces;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

/// <summary>Replaces article values while preventing stale clients from overwriting newer changes.</summary>
/// <param name="repository">The article persistence boundary.</param>
/// <param name="mapper">Maps replacement command values onto the domain article.</param>
public sealed class UpdateArticleHandler(IArticleRepository repository, IMapper mapper)
    : IRequestHandler<UpdateArticleCommand, Article>
{
    /// <summary>Validates and updates an article using optimistic concurrency.</summary>
    /// <param name="command">The identifier, replacement values and the client's last read version.</param>
    /// <param name="cancellationToken">Cancels reading or writing the article.</param>
    /// <returns>The stored article with its new concurrency token.</returns>
    /// <exception cref="ArticleValidationException">The input or version is invalid.</exception>
    /// <exception cref="ArticleNotFoundException">The article does not exist.</exception>
    /// <exception cref="ArticleConflictException">The article changed after the client's last read.</exception>
    public async Task<Article> Handle(UpdateArticleCommand command, CancellationToken cancellationToken)
    {
        var articleId = command.ArticleId;
        ArticleValidator.EnsureValid(command);
        var article = await repository.GetAsync(articleId, cancellationToken)
            ?? throw new ArticleNotFoundException(articleId);
        var expectedVersion = command.Version!.Value;
        if (article.Version != expectedVersion)
            throw new ArticleConflictException(articleId);

        mapper.Map(command, article);
        await repository.UpdateAsync(article, expectedVersion, cancellationToken);
        return article;
    }
}
