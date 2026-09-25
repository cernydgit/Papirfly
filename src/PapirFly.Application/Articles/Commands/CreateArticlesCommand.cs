namespace PapirFly.Application.Articles.Commands;

/// <summary>Requests concurrent creation after validating every article in the batch.</summary>
/// <param name="Articles">The input articles; null entries produce indexed validation errors.</param>
public sealed record CreateArticlesCommand(IReadOnlyList<CreateArticleCommand?> Articles);
