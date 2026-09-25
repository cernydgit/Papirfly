using MapsterMapper;
using MediatR;
using PapirFly.Application.Articles.Commands;
using PapirFly.Application.Interfaces;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

/// <summary>Validates and stores a single article.</summary>
/// <param name="repository">The article persistence boundary.</param>
/// <param name="mapper">Maps command values to the domain article.</param>
public sealed class CreateArticleHandler(IArticleRepository repository, IMapper mapper)
    : IRequestHandler<CreateArticleCommand, Article>
{
    /// <summary>Creates an article and returns its stored representation.</summary>
    /// <param name="command">The article fields to validate and store.</param>
    /// <param name="cancellationToken">Cancels persistence.</param>
    /// <returns>The article with its generated identifier and version.</returns>
    /// <exception cref="ArticleValidationException">The input violates the article contract.</exception>
    public async Task<Article> Handle(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        ArticleValidator.EnsureValid(command);
        var article = mapper.Map<Article>(command);
        await repository.AddAsync(article, cancellationToken);
        return article;
    }
}
