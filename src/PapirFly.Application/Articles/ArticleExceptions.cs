namespace PapirFly.Application.Articles;

public sealed class ArticleValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class ArticleNotFoundException(int articleId)
    : Exception($"Article {articleId} was not found.");

public sealed class ArticleConflictException(int articleId)
    : Exception($"Article {articleId} has changed. Reload it before updating.");
