using MediatR;
using PapirFly.Application.DTOs;

namespace PapirFly.Application.Articles.Queries;

/// <summary>Requests an article by its identifier.</summary>
/// <param name="ArticleId">The identifier to look up.</param>
public sealed record GetArticleQuery(int ArticleId) : IRequest<ArticleResponse>;
