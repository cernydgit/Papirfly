namespace PapirFly.Application.Articles;

public sealed record ArticleDto(
    int ArticleId,
    string Name,
    string Description,
    string? Category,
    decimal Price,
    string? Currency,
    Guid Version);
