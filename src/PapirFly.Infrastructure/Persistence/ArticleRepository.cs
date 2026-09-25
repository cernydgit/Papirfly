using Microsoft.EntityFrameworkCore;
using PapirFly.Application.Articles;
using PapirFly.Application.Interfaces;
using PapirFly.Domain.Articles;

namespace PapirFly.Infrastructure.Persistence;

/// <summary>Persists articles through a separate EF Core context for each operation.</summary>
/// <param name="contextFactory">Creates contexts sharing the host's article store.</param>
public sealed class ArticleRepository(IDbContextFactory<ArticlesDbContext> contextFactory) : IArticleRepository
{
    /// <inheritdoc />
    public async Task<Article?> GetAsync(int articleId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Articles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ArticleId == articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Article>> FindAsync(string? name, string? category, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var articles = context.Articles.AsNoTracking();
        if (name is not null)
            articles = articles.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        if (category is not null)
            articles = articles.Where(x => x.Category == category);

        return await articles.OrderBy(x => x.ArticleId).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Article article, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Articles.Add(article);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task AddConcurrentlyAsync(IReadOnlyList<Article> articles, CancellationToken cancellationToken) =>
        Parallel.ForEachAsync(articles, new ParallelOptions
        {
            MaxDegreeOfParallelism = 4,
            CancellationToken = cancellationToken
        }, async (article, token) => await AddAsync(article, token));

    /// <inheritdoc />
    public async Task UpdateAsync(Article article, Guid expectedVersion, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Articles.Update(article);
        context.Entry(article).Property(x => x.Version).OriginalValue = expectedVersion;
        article.Version = Guid.NewGuid();

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ArticleConflictException(article.ArticleId);
        }
    }
}
