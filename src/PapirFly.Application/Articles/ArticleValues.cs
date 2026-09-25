namespace PapirFly.Application.Articles;

/// <summary>Contains editable values shared by article commands, independent of HTTP contracts.</summary>
public abstract record ArticleValues
{
    /// <summary>Gets the required article name, up to 64 characters.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the required description, up to 2048 characters.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the optional category, up to 64 characters.</summary>
    public string? Category { get; init; }

    /// <summary>Gets the required, non-negative price.</summary>
    public decimal? Price { get; init; }

    /// <summary>Gets the ISO 4217 currency code, required for a positive price.</summary>
    public string? Currency { get; init; }
}
