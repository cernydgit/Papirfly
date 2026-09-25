namespace PapirFly.Api.DTOs;

/// <summary>Contains the editable fields shared by article creation and update requests.</summary>
public abstract record ArticleRequest
{
    /// <summary>Gets the required, nonblank article name, up to 64 characters.</summary>
    public string? Name { get; init; }
    /// <summary>Gets the required, nonblank description, up to 2048 characters.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the optional category, up to 64 characters.</summary>
    public string? Category { get; init; }
    /// <summary>Gets the required, non-negative numeric price.</summary>
    public decimal? Price { get; init; }
    /// <summary>Gets the uppercase ISO 4217 code, required when the price is positive.</summary>
    public string? Currency { get; init; }
}
