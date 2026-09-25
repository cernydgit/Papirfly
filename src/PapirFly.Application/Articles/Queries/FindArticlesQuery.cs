using MediatR;
using PapirFly.Application.DTOs;

namespace PapirFly.Application.Articles.Queries;

/// <summary>Requests articles matching all specified filters.</summary>
/// <param name="Name">An optional case-insensitive name substring.</param>
/// <param name="Category">An optional exact, case-sensitive category.</param>
public sealed record FindArticlesQuery(string? Name = null, string? Category = null) : IRequest<ArticleResponse[]>;
