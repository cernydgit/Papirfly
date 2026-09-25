using MapsterMapper;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

public sealed record CreateArticlesCommand(IReadOnlyList<CreateArticleCommand?> Articles);

public sealed class CreateArticlesHandler(IArticleRepository repository, IMapper mapper)
{
    public async Task<ArticleDto[]> Handle(CreateArticlesCommand command, CancellationToken cancellationToken)
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
        return mapper.Map<ArticleDto[]>(articles);
    }
}
