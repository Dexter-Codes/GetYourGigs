using System.Security.Claims;
using GetYourGigs.Api.Services;
using GetYourGigs.Contracts;
using static GetYourGigs.Api.Endpoints.CurrentAccount;

namespace GetYourGigs.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").RequireRateLimiting("auth");
        auth.MapPost(
                "/email/code",
                async (EmailCodeRequest request, IAuthService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.RequestCodeAsync(request, ct))
            )
            .WithSummary("Email a one-time verification code");
        auth.MapPost(
                "/email/verify",
                async (VerifyEmailRequest request, IAuthService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.VerifyAsync(request, ct))
            )
            .WithSummary("Verify email ownership and sign in");
        auth.MapPost(
                "/google",
                async (GoogleSignInRequest request, IAuthService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.GoogleAsync(request, ct))
            )
            .WithSummary("Verify a Google identity token");
        auth.MapPost(
                "/mobile/exchange",
                async (MobileExchangeRequest request, IAuthService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.ExchangeAsync(request, ct))
            )
            .WithSummary("Redeem a single-use mobile code with its proof key");
        auth.MapPost(
                "/mobile/grant",
                async (
                    ClaimsPrincipal user,
                    MobileGrantRequest request,
                    IAuthService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.MobileGrantAsync(User(user), request, ct))
            )
            .RequireAuthorization()
            .WithSummary("Create a proof-key-bound mobile sign-in handoff");
    }
}
