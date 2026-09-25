using Microsoft.AspNetCore.Mvc;
using PapirFly.Application.Articles;

namespace PapirFly.Api;

public static class ArticleEndpoints
{
    public static void MapArticleEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").WithTags("Articles");

        api.MapPost("/articles", async (CreateArticleCommand command, CreateArticleHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.Handle(command, ct)))
            .WithName("CreateArticle")
            .WithSummary("Register an article.")
            .WithDescription("Name (1-64 characters), description (1-2048), and numeric price >= 0 are required. " +
                "Category is optional (up to 64 characters). Currency must be an uppercase ISO 4217 code and is " +
                "required for a positive price. Do not send article_id or version; the server generates both. " +
                "Returns 200 as specified in the assignment.")
            .Produces<ArticleDto>()
            .ProducesValidationProblem();

        api.MapPost("/articles-concurrent", async ([FromBody] CreateArticleCommand?[] articles,
                CreateArticlesHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.Handle(new(articles), ct)))
            .WithName("CreateArticlesConcurrently")
            .WithSummary("Register an array of articles concurrently.")
            .WithDescription("Uses the same contract as POST /api/articles. Validates every item before any writes; " +
                "an invalid item rejects the entire batch with indexed validation errors. " +
                "Results preserve input order. An empty array returns an empty array. Uses at most four concurrent writers.")
            .Produces<ArticleDto[]>()
            .ProducesValidationProblem();

        api.MapGet("/articles/{articleId:int}", async (int articleId, GetArticleHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.Handle(new(articleId), ct)))
            .WithName("GetArticle")
            .WithSummary("Get an article by its generated ID.")
            .WithDescription("Returns 404 if the ID does not exist. Keep the returned version for the next update.")
            .Produces<ArticleDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        api.MapGet("/articles", async (string? name, string? category, FindArticlesHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.Handle(new(name, category), ct)))
            .WithName("FindArticles")
            .WithSummary("Find articles by name and category.")
            .WithDescription("Name is a case-insensitive substring. Category is an exact, case-sensitive match. " +
                "Both filters are optional and combined with AND. URL-encode query values. " +
                "Returns an array ordered by article_id, or [] when nothing matches.")
            .Produces<ArticleDto[]>();

        api.MapPut("/articles/{articleId:int}", async (int articleId, UpdateArticleCommand command,
                UpdateArticleHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.Handle(articleId, command, ct)))
            .WithName("UpdateArticle")
            .WithSummary("Replace an article using optimistic concurrency.")
            .WithDescription("Send the same fields as POST, plus the version UUID from the last read. " +
                "Missing or empty version returns 400; stale version returns 409. " +
                "A successful update returns the article with a new version. Omitted optional fields are cleared.")
            .Produces<ArticleDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
