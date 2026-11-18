using GetYourGigs.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace GetYourGigs.Tests;

public sealed class ApiExceptionHandlerTests
{
    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, 409)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, 409)]
    [InlineData(PostgresErrorCodes.DiskFull, 500)]
    public async Task Only_expected_database_conflicts_are_reported_as_conflicts(
        string sqlState,
        int expectedStatus
    )
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/gigs";
        var exception = new DbUpdateException(
            "private database detail",
            new PostgresException("private database detail", "ERROR", "ERROR", sqlState)
        );
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("private database detail", body);
        Assert.Contains("traceId", body);
    }
}
