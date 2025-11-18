using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Exceptions;

namespace PaymentGateway.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        (int statusCode, string title) = exception switch
        {
            BaseException e => ((int)e.StatusCode, e.Message),
            HttpRequestException => (503, "External service temporarily unavailable"),
            UnauthorizedAccessException => (401, "Authentication required"),
            TaskCanceledException => (408, "Request timeout"),
            _ => (500, "An unexpected error occurred")
        };

        httpContext.Response.StatusCode = statusCode;
        problemDetails.Status = statusCode;
        problemDetails.Title = title;

        logger.LogError(exception,
            "Exception in {Path}: {ExceptionType}",
            httpContext.Request.Path,
            exception.GetType().Name);

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}