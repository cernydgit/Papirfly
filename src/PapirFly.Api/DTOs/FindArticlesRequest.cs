namespace PapirFly.Api.DTOs;

/// <summary>Contains optional HTTP query parameters used to search for articles.</summary>
public sealed record FindArticlesRequest
{
    /// <summary>Gets the optional case-insensitive name substring.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional exact, case-sensitive category.</summary>
    public string? Category { get; init; }
}
