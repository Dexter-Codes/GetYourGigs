using System.Security.Claims;
using GetYourGigs.Api.Services;
using GetYourGigs.Contracts;
using static GetYourGigs.Api.Endpoints.CurrentAccount;

namespace GetYourGigs.Api.Endpoints;

public static class BookingEndpoints
{
    public static void MapBooking(this RouteGroupBuilder api)
    {
        api.MapPut(
                "/applications/{id:guid}/status",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    ApplicationActionRequest request,
                    IBookingService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.ChangeAsync(User(user), id, request, ct))
            )
            .RequireAuthorization()
            .WithSummary("Shortlist, book, or decline with transactional conflict protection");
        api.MapPost(
                "/applications/{id:guid}/withdraw",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    IBookingService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.CancelAsync(User(user), id, ct))
            )
            .RequireAuthorization()
            .WithSummary("Withdraw your application or upcoming booking");
        api.MapGet(
                "/reviews/{id:guid}",
                async (Guid id, IBookingService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.ReviewByIdAsync(id, ct))
            )
            .WithSummary("Read a genuine completed-gig review");
        api.MapGet(
                "/applications/{id:guid}",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    IBookingService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.ApplicationAsync(User(user), id, ct))
            )
            .RequireAuthorization()
            .WithSummary("Read an application as its applicant or hirer");
        api.MapPost(
                "/reviews",
                async (
                    ClaimsPrincipal user,
                    CreateReviewRequest request,
                    IBookingService service,
                    CancellationToken ct
                ) =>
                {
                    var review = await service.ReviewAsync(User(user), request, ct);
                    return TypedResults.Created($"/api/reviews/{review.Id}", review);
                }
            )
            .RequireAuthorization()
            .WithSummary("Leave one review after completion");
    }
}
