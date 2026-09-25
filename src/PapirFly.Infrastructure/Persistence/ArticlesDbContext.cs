using Microsoft.EntityFrameworkCore;
using PapirFly.Domain.Articles;

namespace PapirFly.Infrastructure.Persistence;

/// <summary>Defines article storage and the application-managed concurrency token.</summary>
/// <param name="options">The configured EF Core provider and database options.</param>
public sealed class ArticlesDbContext(DbContextOptions<ArticlesDbContext> options) : DbContext(options)
{
    /// <summary>Gets the articles stored in this context's database.</summary>
    public DbSet<Article> Articles => Set<Article>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var article = modelBuilder.Entity<Article>();
        article.HasKey(x => x.ArticleId);
        article.Property(x => x.Name).IsRequired().HasMaxLength(Article.NameMaxLength);
        article.Property(x => x.Description).IsRequired().HasMaxLength(Article.DescriptionMaxLength);
        article.Property(x => x.Category).HasMaxLength(Article.CategoryMaxLength);
        article.Property(x => x.Currency).HasMaxLength(3);
        article.Property(x => x.Version).IsConcurrencyToken();
    }
}
