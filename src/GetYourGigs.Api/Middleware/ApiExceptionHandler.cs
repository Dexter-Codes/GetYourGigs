using GetYourGigs.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GetYourGigs.Api.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct
    )
    {
        var status = exception switch
        {
            ApiException e => e.Status,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                        or PostgresErrorCodes.ForeignKeyViolation
                }
            } => StatusCodes.Status409Conflict,
            _ => 500,
        };
        var detail =
            exception is ApiException known ? known.Message
            : status == 409 ? "This item changed. Refresh and try again."
            : "Something went wrong. Please try again.";
        if (status >= 500)
            logger.LogError(exception, "Unhandled request failure");
        else
            logger.LogInformation("Request rejected with {Status}: {Detail}", status, detail);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = status >= 500 ? "Server error" : "Request could not be completed",
                Detail = detail,
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier },
            },
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json",
            cancellationToken: ct
        );
        return true;
    }
}
