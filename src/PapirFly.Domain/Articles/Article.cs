namespace PapirFly.Domain.Articles;

public sealed class Article
{
    public const int NameMaxLength = 64;
    public const int DescriptionMaxLength = 2048;
    public const int CategoryMaxLength = 64;

    public int ArticleId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
