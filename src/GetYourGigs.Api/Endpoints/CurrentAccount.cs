using System.Security.Claims;
using GetYourGigs.Api.Services;

namespace GetYourGigs.Api.Endpoints;

internal static class CurrentAccount
{
    public static Guid User(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out var accountId)
            ? accountId
            : throw new ApiException(
                StatusCodes.Status401Unauthorized,
                "A valid account is required."
            );
}
