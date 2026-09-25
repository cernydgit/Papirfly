using PapirFly.Application.DTOs;

namespace PapirFly.Application.Articles.Commands;

/// <summary>Requests replacement of an article using the version returned by the last read.</summary>
public sealed record UpdateArticleCommand : ArticleInput
{
    /// <summary>Gets the required, nonempty concurrency token returned when the article was read.</summary>
    public Guid? Version { get; init; }
}
