using MediatR;
using PapirFly.Domain.Articles;

namespace PapirFly.Application.Articles.Commands;

/// <summary>Requests creation of an article with a server-generated identifier and version.</summary>
public sealed record CreateArticleCommand : ArticleValues, IRequest<Article>;
