using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PapirFly.Application.Articles;

namespace PapirFly.Api;

/// <summary>Converts expected application failures into HTTP Problem Details responses.</summary>
/// <param name="problemDetailsService">Writes the configured Problem Details response.</param>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <summary>Handles validation, missing article, concurrency and HTTP request failures.</summary>
    /// <param name="context">The request whose response is being written.</param>
    /// <param name="exception">The failure to translate.</param>
    /// <param name="cancellationToken">Signals request cancellation.</param>
    /// <returns>True when the failure was handled; otherwise false so the default handler can process it.</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            ArticleValidationException validation => new HttpValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest
            },
            ArticleNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Article not found",
                Detail = exception.Message
            },
            ArticleConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Article version conflict",
                Detail = exception.Message
            },
            BadHttpRequestException request => new ProblemDetails
            {
                Status = request.StatusCode,
                Title = ReasonPhrases.GetReasonPhrase(request.StatusCode)
            },
            _ => null
        };

        if (problem is null)
            return false;

        context.Response.StatusCode = problem.Status!.Value;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
        return true;
    }
}
