namespace PapirFly.Api.DTOs;

/// <summary>Represents a stored article returned by the HTTP API.</summary>
/// <param name="ArticleId">The positive, server-generated identifier.</param>
/// <param name="Name">The article name.</param>
/// <param name="Description">The article description.</param>
/// <param name="Category">The optional category.</param>
/// <param name="Price">The non-negative price.</param>
/// <param name="Currency">The ISO 4217 code, or null for a free article without a currency.</param>
/// <param name="Version">The concurrency token to submit with the next update.</param>
public sealed record ArticleResponse(
    int ArticleId,
    string Name,
    string Description,
    string? Category,
    decimal Price,
    string? Currency,
    Guid Version);
