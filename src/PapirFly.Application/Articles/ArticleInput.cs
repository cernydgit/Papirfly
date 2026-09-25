namespace PapirFly.Application.Articles;

public abstract record ArticleInput
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
}
