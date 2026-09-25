using MediatR;
using PapirFly.Application.DTOs;

namespace PapirFly.Application.Articles.Commands;

/// <summary>Requests replacement of an article using the version returned by the last read.</summary>
/// <param name="ArticleId">The identifier supplied by the route.</param>
/// <param name="Article">The replacement values and the client's last read version.</param>
public sealed record UpdateArticleCommand(int ArticleId, UpdateArticleRequest Article) : IRequest<ArticleResponse>;
