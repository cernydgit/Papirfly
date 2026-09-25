namespace PapirFly.Domain.Articles;

/// <summary>Represents a shop article and its current concurrency version.</summary>
public sealed class Article
{
    /// <summary>The maximum article name length.</summary>
    public const int NameMaxLength = 64;
    /// <summary>The maximum article description length.</summary>
    public const int DescriptionMaxLength = 2048;
    /// <summary>The maximum category length.</summary>
    public const int CategoryMaxLength = 64;

    /// <summary>Gets or sets the server-generated article identifier.</summary>
    public int ArticleId { get; set; }
    /// <summary>Gets or sets the article name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Gets or sets the article description.</summary>
    public string Description { get; set; } = "";
    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }
    /// <summary>Gets or sets the non-negative price.</summary>
    public decimal Price { get; set; }
    /// <summary>Gets or sets the ISO 4217 currency code, if supplied.</summary>
    public string? Currency { get; set; }
    /// <summary>Gets or sets the application-managed optimistic concurrency token.</summary>
    public Guid Version { get; set; } = Guid.NewGuid();
}
