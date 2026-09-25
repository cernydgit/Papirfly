using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles;

public interface IArticleRepository
{
    Task<Article?> GetAsync(int articleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Article>> FindAsync(string? name, string? category, CancellationToken cancellationToken);
    Task AddAsync(Article article, CancellationToken cancellationToken);
    Task AddConcurrentlyAsync(IReadOnlyList<Article> articles, CancellationToken cancellationToken);
    Task UpdateAsync(Article article, Guid expectedVersion, CancellationToken cancellationToken);
}
