using PapirFly.Application.Articles;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Interfaces;

/// <summary>Provides the article persistence operations required by application use cases.</summary>
public interface IArticleRepository
{
    /// <summary>Reads an article without tracking subsequent changes.</summary>
    /// <param name="articleId">The article identifier.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A detached article, or null if the identifier does not exist.</returns>
    Task<Article?> GetAsync(int articleId, CancellationToken cancellationToken);

    /// <summary>Finds articles matching all supplied filters.</summary>
    /// <param name="name">An optional case-insensitive name substring.</param>
    /// <param name="category">An optional exact, case-sensitive category.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    /// <returns>Detached articles ordered by identifier; an empty list when none match.</returns>
    Task<IReadOnlyList<Article>> FindAsync(string? name, string? category, CancellationToken cancellationToken);

    /// <summary>Stores an article and assigns its generated identifier to the supplied entity.</summary>
    /// <param name="article">The validated article to insert.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the article has been stored.</returns>
    Task AddAsync(Article article, CancellationToken cancellationToken);

    /// <summary>Stores validated articles concurrently, updating each entity with its generated identifier.</summary>
    /// <remarks>Storage failures or cancellation may leave partial writes; the operation is not transactional.</remarks>
    /// <param name="articles">The fully validated batch, whose ordering is preserved.</param>
    /// <param name="cancellationToken">Cancels outstanding writes.</param>
    /// <returns>A task that completes when every article has been stored.</returns>
    Task AddConcurrentlyAsync(IReadOnlyList<Article> articles, CancellationToken cancellationToken);

    /// <summary>Replaces an article only if its stored version still matches, then assigns a new version.</summary>
    /// <param name="article">The validated article containing the replacement values.</param>
    /// <param name="expectedVersion">The concurrency token from the client's last read.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes after the version check and update succeed.</returns>
    /// <exception cref="ArticleConflictException">The stored article no longer matches the expected version.</exception>
    Task UpdateAsync(Article article, Guid expectedVersion, CancellationToken cancellationToken);
}
