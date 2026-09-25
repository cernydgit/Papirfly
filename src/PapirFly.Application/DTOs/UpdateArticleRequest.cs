namespace PapirFly.Application.DTOs;

/// <summary>Contains replacement article values; the identifier is supplied separately by the route.</summary>
public sealed record UpdateArticleRequest : ArticleRequest
{
    /// <summary>Gets the required, nonempty concurrency token returned when the article was read.</summary>
    public Guid? Version { get; init; }
}
