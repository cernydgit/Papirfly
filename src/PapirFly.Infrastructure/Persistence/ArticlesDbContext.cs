using Microsoft.EntityFrameworkCore;
using PapirFly.Domain.Articles;

namespace PapirFly.Infrastructure.Persistence;

public sealed class ArticlesDbContext(DbContextOptions<ArticlesDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

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
