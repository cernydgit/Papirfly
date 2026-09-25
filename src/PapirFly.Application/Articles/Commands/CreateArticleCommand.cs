using MediatR;
using PapirFly.Application.DTOs;

namespace PapirFly.Application.Articles.Commands;

/// <summary>Requests creation of an article with a server-generated identifier and version.</summary>
public sealed record CreateArticleCommand : ArticleRequest, IRequest<ArticleResponse>;
