namespace PapirFly.Api.DTOs;

/// <summary>Contains the HTTP body for creating an article without a client-supplied identifier or version.</summary>
public sealed record CreateArticleRequest : ArticleRequest;
