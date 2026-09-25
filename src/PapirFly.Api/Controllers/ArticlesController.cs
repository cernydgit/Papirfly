using MediatR;
using Microsoft.AspNetCore.Mvc;
using PapirFly.Application.Articles.Commands;
using PapirFly.Application.Articles.Queries;
using PapirFly.Application.DTOs;

namespace PapirFly.Api.Controllers;

/// <summary>Registers, retrieves and updates shop articles.</summary>
/// <param name="sender">Dispatches commands and queries to their application handlers through MediatR.</param>
[ApiController]
[Route("api/articles")]
[Produces("application/json")]
public sealed class ArticlesController(ISender sender) : ControllerBase
{
    /// <summary>Registers an article.</summary>
    /// <remarks>
    /// Name (1-64 characters), description (1-2048), and numeric price greater than or equal to zero are required.
    /// Category is optional (up to 64 characters). Currency must be an uppercase ISO 4217 code and is required
    /// for a positive price. Do not send article_id or version; the server generates both.
    /// Returns 200 as specified in the assignment.
    /// </remarks>
    /// <param name="command">The article fields to validate and store.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The stored article with its generated identifier and version.</returns>
    /// <response code="200">The article was stored.</response>
    /// <response code="400">The request body or article fields are invalid.</response>
    [HttpPost(Name = "CreateArticle")]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleResponse>> Create(
        [FromBody] CreateArticleCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    /// <summary>Registers an array of articles concurrently.</summary>
    /// <remarks>
    /// Uses the same contract as POST /api/articles. Validates every item before any writes; an invalid item
    /// rejects the entire batch with indexed validation errors. Results preserve input order. An empty array
    /// returns an empty array. Uses at most four concurrent writers.
    /// </remarks>
    /// <param name="articles">The ordered array of articles to register.</param>
    /// <param name="cancellationToken">Cancels outstanding writes.</param>
    /// <returns>The stored articles in input order.</returns>
    /// <response code="200">All articles were stored.</response>
    /// <response code="400">The request body or at least one article is invalid.</response>
    [HttpPost("/api/articles-concurrent", Name = "CreateArticlesConcurrently")]
    [ProducesResponseType<ArticleResponse[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleResponse[]>> CreateConcurrently(
        [FromBody] CreateArticleCommand?[] articles, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CreateArticlesCommand(articles), cancellationToken));

    /// <summary>Gets an article by its generated identifier.</summary>
    /// <remarks>Returns 404 if the identifier does not exist. Keep the returned version for the next update.</remarks>
    /// <param name="articleId">The identifier to look up.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The current article values and concurrency token.</returns>
    /// <response code="200">The requested article.</response>
    /// <response code="404">The article does not exist.</response>
    [HttpGet("{articleId:int}", Name = "GetArticle")]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleResponse>> Get(int articleId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetArticleQuery(articleId), cancellationToken));

    /// <summary>Finds articles by name and category.</summary>
    /// <remarks>
    /// Both filters are optional and combined with AND. URL-encode query values. Without filters, returns all
    /// articles ordered by article_id. Returns an empty array when nothing matches.
    /// </remarks>
    /// <param name="name">An optional case-insensitive name substring.</param>
    /// <param name="category">An optional exact, case-sensitive category.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    /// <returns>Matching articles ordered by identifier.</returns>
    /// <response code="200">The matching articles, possibly an empty array.</response>
    [HttpGet(Name = "FindArticles")]
    [ProducesResponseType<ArticleResponse[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArticleResponse[]>> Find(
        [FromQuery] string? name, [FromQuery] string? category, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new FindArticlesQuery(name, category), cancellationToken));

    /// <summary>Replaces an article using optimistic concurrency.</summary>
    /// <remarks>
    /// Send the same fields as POST, plus the version UUID from the last read. A missing or empty version
    /// returns 400; a stale version returns 409. A successful update returns the article with a new version.
    /// Omitted optional fields are cleared.
    /// </remarks>
    /// <param name="articleId">The identifier of the article to replace.</param>
    /// <param name="request">The replacement values and the version from the last read.</param>
    /// <param name="cancellationToken">Cancels the read or write.</param>
    /// <returns>The updated article with its new concurrency token.</returns>
    /// <response code="200">The article was updated.</response>
    /// <response code="400">The request body, article fields or version are invalid.</response>
    /// <response code="404">The article does not exist.</response>
    /// <response code="409">The article has changed since the client's last read.</response>
    [HttpPut("{articleId:int}", Name = "UpdateArticle")]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ArticleResponse>> Update(
        int articleId, [FromBody] UpdateArticleRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateArticleCommand(articleId, request), cancellationToken));
}
