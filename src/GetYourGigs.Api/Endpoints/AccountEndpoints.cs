using System.Security.Claims;
using GetYourGigs.Api.Services;
using GetYourGigs.Contracts;
using static GetYourGigs.Api.Endpoints.CurrentAccount;

namespace GetYourGigs.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccount(this RouteGroupBuilder api)
    {
        var me = api.MapGroup("/me").RequireAuthorization();
        me.MapGet(
                "/",
                async (ClaimsPrincipal user, IAuthService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.AccountAsync(User(user), ct))
            )
            .WithSummary("Get the current account");
        me.MapPut(
                "/mode",
                async (
                    ClaimsPrincipal user,
                    UpdateModeRequest request,
                    IAuthService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.ModeAsync(User(user), request, ct))
            )
            .WithSummary("Remember Hiring or Working context");
        me.MapPut(
                "/",
                async (
                    ClaimsPrincipal user,
                    UpdateAccountRequest request,
                    IAuthService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.UpdateAsync(User(user), request, ct))
            )
            .WithSummary("Update your display name");
        me.MapGet(
                "/profile",
                async (ClaimsPrincipal user, ICatalogService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.MyProfileAsync(User(user), ct))
            )
            .WithSummary("Get your talent profile");
        me.MapPut(
                "/profile",
                async (
                    ClaimsPrincipal user,
                    SaveProfileRequest request,
                    ICatalogService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.SaveProfileAsync(User(user), request, ct))
            )
            .WithSummary("Publish or update your talent profile");
        me.MapGet(
                "/gigs",
                async (ClaimsPrincipal user, ICatalogService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.MineAsync(User(user), ct))
            )
            .WithSummary("Get your own posted gigs");
        me.MapGet(
                "/applications",
                async (ClaimsPrincipal user, IBookingService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.ApplicationsAsync(User(user), null, ct))
            )
            .WithSummary("Get your applications");
        me.MapGet(
                "/allowance",
                async (ClaimsPrincipal user, IBookingService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.AllowanceAsync(User(user), ct))
            )
            .WithSummary("Get today'service remaining application allowance");
        me.MapGet(
                "/bookmarks/{id:guid}",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    ICatalogService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.BookmarkByIdAsync(User(user), id, ct))
            )
            .WithSummary("Read a saved listing belonging to you");
        me.MapGet(
                "/bookmarks",
                async (ClaimsPrincipal user, ICatalogService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.BookmarksAsync(User(user), ct))
            )
            .WithSummary("Get saved talent and gigs");
        me.MapPost(
                "/bookmarks",
                async (
                    ClaimsPrincipal user,
                    SaveBookmarkRequest request,
                    ICatalogService service,
                    CancellationToken ct
                ) =>
                {
                    var b = await service.BookmarkAsync(User(user), request, ct);
                    return TypedResults.Created($"/api/me/bookmarks/{b.Id}", b);
                }
            )
            .WithSummary("Save a listing");
        me.MapDelete(
                "/bookmarks/{id:guid}",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    ICatalogService service,
                    CancellationToken ct
                ) =>
                {
                    await service.RemoveBookmarkAsync(User(user), id, ct);
                    return TypedResults.NoContent();
                }
            )
            .WithSummary("Remove a saved listing");
        me.MapGet(
                "/updates",
                async (ClaimsPrincipal user, IMessagingService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.UpdatesAsync(User(user), ct))
            )
            .WithSummary("Get relevant activity while you were away");
        me.MapPost(
                "/updates/read",
                async (ClaimsPrincipal user, IMessagingService service, CancellationToken ct) =>
                {
                    await service.ReadUpdatesAsync(User(user), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithSummary("Mark your activity read");
    }
}
