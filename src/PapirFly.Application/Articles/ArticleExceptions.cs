namespace PapirFly.Application.Articles;

/// <summary>Reports all validation errors for an article or batch.</summary>
/// <param name="errors">Errors keyed by contract field name, optionally prefixed by a batch index.</param>
public sealed class ArticleValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    /// <summary>Gets the field-level validation errors.</summary>
    public IDictionary<string, string[]> Errors { get; } = errors;
}

/// <summary>Indicates that a requested article does not exist.</summary>
/// <param name="articleId">The missing article identifier.</param>
public sealed class ArticleNotFoundException(int articleId)
    : Exception($"Article {articleId} was not found.");

/// <summary>Indicates that an article no longer has the version supplied by the client.</summary>
/// <param name="articleId">The article whose version conflicted.</param>
public sealed class ArticleConflictException(int articleId)
    : Exception($"Article {articleId} has changed. Reload it before updating.");
