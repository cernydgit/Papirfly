using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PapirFly.Application.Articles;

namespace PapirFly.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
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
