using MediatR;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles.Commands;

/// <summary>Requests replacement of an article using the version returned by the last read.</summary>
public sealed record UpdateArticleCommand : ArticleValues, IRequest<Article>
{
    /// <summary>Gets the identifier of the article to replace.</summary>
    public int ArticleId { get; init; }

    /// <summary>Gets the required, nonempty version from the client's last read.</summary>
    public Guid? Version { get; init; }
}
