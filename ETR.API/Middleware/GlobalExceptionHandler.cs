using ETR.Application.Compliance;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ETR.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    // SQL Server error numbers: 2601/2627 = unique-index/constraint violation, 547 = FK constraint violation.
    private const int SqlUniqueViolation1 = 2601;
    private const int SqlUniqueViolation2 = 2627;
    private const int SqlForeignKeyViolation = 547;

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = Classify(exception);

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
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }

    // Only BusinessRuleViolationException/ForbiddenAccessException/KeyNotFoundException/ValidationException
    // messages are echoed to the client — they are hand-authored by our own services to be client-safe.
    // Every other exception type (including bare InvalidOperationException from EF Core/LINQ/BCL) gets a
    // fixed, generic detail so framework internals never leak through the response.
    private static (int StatusCode, string Title, string Detail) Classify(Exception exception) => exception switch
    {
        BusinessRuleViolationException => (StatusCodes.Status400BadRequest, "Business rule violation", exception.Message),
        ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Not authenticated", "Authentication is required to perform this action."),
        ValidationException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
        DbUpdateException { InnerException: SqlException { Number: SqlUniqueViolation1 or SqlUniqueViolation2 } }
            => (StatusCodes.Status409Conflict, "Conflict", "A record with the same unique value already exists."),
        DbUpdateException { InnerException: SqlException { Number: SqlForeignKeyViolation } }
            => (StatusCodes.Status400BadRequest, "Invalid reference", "One or more referenced resources do not exist."),
        DbUpdateException => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "An unexpected error occurred."),
        IOException => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "A storage operation failed. Please retry."),
        NotSupportedException => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "An unexpected error occurred."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "An unexpected error occurred.")
    };
}
