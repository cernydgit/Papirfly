using MapsterMapper;
using MediatR;
using PapirFly.Application.Articles.Commands;
using PapirFly.Application.Interfaces;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

/// <summary>Validates an entire batch before storing its articles concurrently.</summary>
/// <param name="repository">The article persistence boundary.</param>
/// <param name="mapper">Maps command values to domain articles.</param>
public sealed class CreateArticlesHandler(IArticleRepository repository, IMapper mapper)
    : IRequestHandler<CreateArticlesCommand, Article[]>
{
    /// <summary>Creates the batch after validating all its items.</summary>
    /// <param name="command">The ordered batch of input articles.</param>
    /// <param name="cancellationToken">Cancels outstanding writes.</param>
    /// <returns>The stored articles in input order, including their generated identifiers and versions.</returns>
    /// <exception cref="ArticleValidationException">An item is invalid; no writes are started.</exception>
    public async Task<Article[]> Handle(CreateArticlesCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        for (var i = 0; i < command.Articles.Count; i++)
        {
            foreach (var error in ArticleValidator.Validate(command.Articles[i]))
                errors[$"[{i}].{error.Key}"] = error.Value;
        }

        // Validate the whole batch before allowing any writes.
        if (errors.Count > 0)
            throw new ArticleValidationException(errors);

        var articles = mapper.Map<Article[]>(command.Articles);
        await repository.AddConcurrentlyAsync(articles, cancellationToken);
        return articles;
    }
}
