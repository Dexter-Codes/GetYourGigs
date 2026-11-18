using System.Security.Claims;
using GetYourGigs.Api.Services;
using GetYourGigs.Contracts;
using static GetYourGigs.Api.Endpoints.CurrentAccount;

namespace GetYourGigs.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalog(this RouteGroupBuilder api)
    {
        api.MapGet(
                "/profiles",
                async (
                    ClaimsPrincipal user,
                    string? q,
                    string? category,
                    string? city,
                    bool? available,
                    int? page,
                    ICatalogService service,
                    CancellationToken ct
                ) =>
                    TypedResults.Ok(
                        await service.ProfilesAsync(
                            user.Identity?.IsAuthenticated == true,
                            q,
                            category,
                            city,
                            available,
                            page ?? 1,
                            ct
                        )
                    )
            )
            .WithSummary("Discover talent; anonymous previews are capped server-side");
        api.MapGet(
                "/profiles/{id:guid}",
                async (Guid id, ICatalogService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.ProfileAsync(id, ct))
            )
            .WithSummary("Read a public talent listing");
        api.MapGet(
                "/profiles/{id:guid}/reviews",
                async (Guid id, IBookingService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.ReviewsAsync(id, ct))
            )
            .WithSummary("Read genuine completed-gig reviews");
        api.MapGet(
                "/gigs",
                async (
                    ClaimsPrincipal user,
                    string? q,
                    string? category,
                    string? city,
                    int? page,
                    ICatalogService service,
                    CancellationToken ct
                ) =>
                    TypedResults.Ok(
                        await service.GigsAsync(
                            user.Identity?.IsAuthenticated == true,
                            q,
                            category,
                            city,
                            page ?? 1,
                            ct
                        )
                    )
            )
            .WithSummary("Discover open gigs with anonymous preview gating");
        api.MapGet(
                "/gigs/{id:guid}",
                async (Guid id, ICatalogService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.GigAsync(id, ct))
            )
            .WithSummary("Read a gig'service details");
        var gigs = api.MapGroup("/gigs").RequireAuthorization();
        gigs.MapPost(
                "/",
                async (
                    ClaimsPrincipal user,
                    SaveGigRequest request,
                    ICatalogService service,
                    CancellationToken ct
                ) =>
                {
                    var g = await service.SaveGigAsync(User(user), null, request, ct);
                    return TypedResults.Created($"/api/gigs/{g.Id}", g);
                }
            )
            .WithSummary("Post a gig");
        gigs.MapPut(
                "/{id:guid}",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    SaveGigRequest request,
                    ICatalogService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.SaveGigAsync(User(user), id, request, ct))
            )
            .WithSummary("Edit your open gig");
        gigs.MapPost(
                "/{id:guid}/close",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    ICatalogService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.CloseAsync(User(user), id, ct))
            )
            .WithSummary("Close your open gig and notify applicants");
        gigs.MapPost(
                "/{id:guid}/complete",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    IBookingService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.CompleteAsync(User(user), id, ct))
            )
            .WithSummary("Confirm a booked gig has actually completed");
        gigs.MapGet(
                "/{id:guid}/applications",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    IBookingService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.ApplicationsAsync(User(user), id, ct))
            )
            .WithSummary("Review applicants to your gig");
        gigs.MapPost(
                "/{id:guid}/applications",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    ApplyRequest request,
                    IBookingService service,
                    CancellationToken ct
                ) =>
                {
                    var a = await service.ApplyAsync(User(user), id, request, ct);
                    return TypedResults.Created($"/api/applications/{a.Id}", a);
                }
            )
            .WithSummary("Apply to a gig, up to three times daily");
    }
}
