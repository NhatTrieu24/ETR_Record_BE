using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ETR.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Not authenticated"),
            ValidationException => (StatusCodes.Status400BadRequest, "Invalid request"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Business rule violation"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception ({StatusCode}) while processing {Path}", statusCode, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}
