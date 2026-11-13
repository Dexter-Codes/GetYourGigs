using System.Security.Claims;
using GetYourGigs.Api.Hosting;
using GetYourGigs.Api.Services;
using GetYourGigs.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Endpoints;

public static class ApiEndpoints
{
    private static Guid User(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("sub")!);

    public static void MapMarketplace(this WebApplication app)
    {
        if (HostedConfiguration.IsSupabase(app.Configuration))
            app.MapGet(
                    "/uploads/{name}",
                    Results<RedirectHttpResult, NotFound> (string name, IImageStorage storage) =>
                        storage.PublicUrl(name) is { } url
                            ? TypedResults.Redirect(url)
                            : TypedResults.NotFound()
                )
                .WithSummary("Read a previously uploaded public image from persistent storage");

        var api = app.MapGroup("/api").RequireRateLimiting("api");
        api.MapGet(
                "/config",
                (IConfiguration c, IEmailCodeSender sender) =>
                    TypedResults.Ok(
                        new PublicConfigResponse(
                            c["Google:ClientId"],
                            sender.UsesDevelopmentCode,
                            Categories.All
                        )
                    )
            )
            .WithSummary("Public sign-in configuration");
        var auth = api.MapGroup("/auth").RequireRateLimiting("auth");
        auth.MapPost(
                "/email/code",
                async (EmailCodeRequest r, IAuthService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.RequestCodeAsync(r, ct))
            )
            .WithSummary("Email a one-time verification code");
        auth.MapPost(
                "/email/verify",
                async (VerifyEmailRequest r, IAuthService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.VerifyAsync(r, ct))
            )
            .WithSummary("Verify email ownership and sign in");
        auth.MapPost(
                "/google",
                async (GoogleSignInRequest r, IAuthService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.GoogleAsync(r, ct))
            )
            .WithSummary("Verify a Google identity token");
        auth.MapPost(
                "/mobile/exchange",
                async (MobileExchangeRequest r, IAuthService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.ExchangeAsync(r, ct))
            )
            .WithSummary("Redeem a single-use mobile code with its proof key");
        auth.MapPost(
                "/mobile/grant",
                async (
                    ClaimsPrincipal u,
                    MobileGrantRequest r,
                    IAuthService s,
                    CancellationToken ct
                ) => TypedResults.Ok(await s.MobileGrantAsync(User(u), r, ct))
            )
            .RequireAuthorization()
            .WithSummary("Create a proof-key-bound mobile sign-in handoff");
        var me = api.MapGroup("/me").RequireAuthorization();
        me.MapGet(
                "/",
                async (ClaimsPrincipal u, IAuthService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.AccountAsync(User(u), ct))
            )
            .WithSummary("Get the current account");
        me.MapPut(
                "/mode",
                async (
                    ClaimsPrincipal u,
                    UpdateModeRequest r,
                    IAuthService s,
                    CancellationToken ct
                ) => TypedResults.Ok(await s.ModeAsync(User(u), r, ct))
            )
            .WithSummary("Remember Hiring or Working context");
        me.MapPut(
                "/",
                async (
                    ClaimsPrincipal u,
                    UpdateAccountRequest r,
                    IAuthService s,
                    CancellationToken ct
                ) => TypedResults.Ok(await s.UpdateAsync(User(u), r, ct))
            )
            .WithSummary("Update your display name");
        me.MapGet(
                "/profile",
                async (ClaimsPrincipal u, ICatalogService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.MyProfileAsync(User(u), ct))
            )
            .WithSummary("Get your talent profile");
        me.MapPut(
                "/profile",
                async (
                    ClaimsPrincipal u,
                    SaveProfileRequest r,
                    ICatalogService s,
                    CancellationToken ct
                ) => TypedResults.Ok(await s.SaveProfileAsync(User(u), r, ct))
            )
            .WithSummary("Publish or update your talent profile");
        me.MapGet(
                "/gigs",
                async (ClaimsPrincipal u, ICatalogService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.MineAsync(User(u), ct))
            )
            .WithSummary("Get your own posted gigs");
        me.MapGet(
                "/applications",
                async (ClaimsPrincipal u, IBookingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.ApplicationsAsync(User(u), null, ct))
            )
            .WithSummary("Get your applications");
        me.MapGet(
                "/allowance",
                async (ClaimsPrincipal u, IBookingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.AllowanceAsync(User(u), ct))
            )
            .WithSummary("Get today's remaining application allowance");
        me.MapGet(
                "/bookmarks/{id:guid}",
                async (ClaimsPrincipal u, Guid id, ICatalogService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.BookmarkByIdAsync(User(u), id, ct))
            )
            .WithSummary("Read a saved listing belonging to you");
        me.MapGet(
                "/bookmarks",
                async (ClaimsPrincipal u, ICatalogService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.BookmarksAsync(User(u), ct))
            )
            .WithSummary("Get saved talent and gigs");
        me.MapPost(
                "/bookmarks",
                async (
                    ClaimsPrincipal u,
                    SaveBookmarkRequest r,
                    ICatalogService s,
                    CancellationToken ct
                ) =>
                {
                    var b = await s.BookmarkAsync(User(u), r, ct);
                    return TypedResults.Created($"/api/me/bookmarks/{b.Id}", b);
                }
            )
            .WithSummary("Save a listing");
        me.MapDelete(
                "/bookmarks/{id:guid}",
                async (ClaimsPrincipal u, Guid id, ICatalogService s, CancellationToken ct) =>
                {
                    await s.RemoveBookmarkAsync(User(u), id, ct);
                    return TypedResults.NoContent();
                }
            )
            .WithSummary("Remove a saved listing");
        me.MapGet(
                "/updates",
                async (ClaimsPrincipal u, IMessagingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.UpdatesAsync(User(u), ct))
            )
            .WithSummary("Get relevant activity while you were away");
        me.MapPost(
                "/updates/read",
                async (ClaimsPrincipal u, IMessagingService s, CancellationToken ct) =>
                {
                    await s.ReadUpdatesAsync(User(u), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithSummary("Mark your activity read");
        api.MapGet(
                "/profiles",
                async (
                    ClaimsPrincipal u,
                    string? q,
                    string? category,
                    string? city,
                    bool? available,
                    int? page,
                    ICatalogService s,
                    CancellationToken ct
                ) =>
                    TypedResults.Ok(
                        await s.ProfilesAsync(
                            u.Identity?.IsAuthenticated == true,
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
                async (Guid id, ICatalogService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.ProfileAsync(id, ct))
            )
            .WithSummary("Read a public talent listing");
        api.MapGet(
                "/profiles/{id:guid}/reviews",
                async (Guid id, IBookingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.ReviewsAsync(id, ct))
            )
            .WithSummary("Read genuine completed-gig reviews");
        api.MapGet(
                "/gigs",
                async (
                    ClaimsPrincipal u,
                    string? q,
                    string? category,
                    string? city,
                    int? page,
                    ICatalogService s,
                    CancellationToken ct
                ) =>
                    TypedResults.Ok(
                        await s.GigsAsync(
                            u.Identity?.IsAuthenticated == true,
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
                async (Guid id, ICatalogService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.GigAsync(id, ct))
            )
            .WithSummary("Read a gig's details");
        var gigs = api.MapGroup("/gigs").RequireAuthorization();
        gigs.MapPost(
                "/",
                async (
                    ClaimsPrincipal u,
                    SaveGigRequest r,
                    ICatalogService s,
                    CancellationToken ct
                ) =>
                {
                    var g = await s.SaveGigAsync(User(u), null, r, ct);
                    return TypedResults.Created($"/api/gigs/{g.Id}", g);
                }
            )
            .WithSummary("Post a gig");
        gigs.MapPut(
                "/{id:guid}",
                async (
                    ClaimsPrincipal u,
                    Guid id,
                    SaveGigRequest r,
                    ICatalogService s,
                    CancellationToken ct
                ) => TypedResults.Ok(await s.SaveGigAsync(User(u), id, r, ct))
            )
            .WithSummary("Edit your open gig");
        gigs.MapPost(
                "/{id:guid}/close",
                async (ClaimsPrincipal u, Guid id, ICatalogService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.CloseAsync(User(u), id, ct))
            )
            .WithSummary("Close your open gig and notify applicants");
        gigs.MapPost(
                "/{id:guid}/complete",
                async (ClaimsPrincipal u, Guid id, IBookingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.CompleteAsync(User(u), id, ct))
            )
            .WithSummary("Confirm a booked gig has actually completed");
        gigs.MapGet(
                "/{id:guid}/applications",
                async (ClaimsPrincipal u, Guid id, IBookingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.ApplicationsAsync(User(u), id, ct))
            )
            .WithSummary("Review applicants to your gig");
        gigs.MapPost(
                "/{id:guid}/applications",
                async (
                    ClaimsPrincipal u,
                    Guid id,
                    ApplyRequest r,
                    IBookingService s,
                    CancellationToken ct
                ) =>
                {
                    var a = await s.ApplyAsync(User(u), id, r, ct);
                    return TypedResults.Created($"/api/applications/{a.Id}", a);
                }
            )
            .WithSummary("Apply to a gig, up to three times daily");
        api.MapPut(
                "/applications/{id:guid}/status",
                async (
                    ClaimsPrincipal u,
                    Guid id,
                    ApplicationActionRequest r,
                    IBookingService s,
                    CancellationToken ct
                ) => TypedResults.Ok(await s.ChangeAsync(User(u), id, r, ct))
            )
            .RequireAuthorization()
            .WithSummary("Shortlist, book, or decline with transactional conflict protection");
        api.MapPost(
                "/applications/{id:guid}/withdraw",
                async (ClaimsPrincipal u, Guid id, IBookingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.CancelAsync(User(u), id, ct))
            )
            .RequireAuthorization()
            .WithSummary("Withdraw your application or upcoming booking");
        api.MapGet(
                "/reviews/{id:guid}",
                async (Guid id, IBookingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.ReviewByIdAsync(id, ct))
            )
            .WithSummary("Read a genuine completed-gig review");
        api.MapGet(
                "/applications/{id:guid}",
                async (ClaimsPrincipal u, Guid id, IBookingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.ApplicationAsync(User(u), id, ct))
            )
            .RequireAuthorization()
            .WithSummary("Read an application as its applicant or hirer");
        api.MapPost(
                "/reviews",
                async (
                    ClaimsPrincipal u,
                    CreateReviewRequest r,
                    IBookingService s,
                    CancellationToken ct
                ) =>
                {
                    var review = await s.ReviewAsync(User(u), r, ct);
                    return TypedResults.Created($"/api/reviews/{review.Id}", review);
                }
            )
            .RequireAuthorization()
            .WithSummary("Leave one review after completion");
        var threads = api.MapGroup("/threads").RequireAuthorization();
        threads
            .MapGet(
                "/",
                async (ClaimsPrincipal u, IMessagingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.ThreadsAsync(User(u), ct))
            )
            .WithSummary("Get your private inbox");
        threads
            .MapPost(
                "/",
                async (
                    ClaimsPrincipal u,
                    StartThreadRequest r,
                    IMessagingService s,
                    CancellationToken ct
                ) =>
                {
                    var t = await s.StartAsync(User(u), r, ct);
                    return TypedResults.Created($"/api/threads/{t.Id}/messages", t);
                }
            )
            .WithSummary("Start or resume a conversation");
        threads
            .MapGet(
                "/{id:guid}/messages",
                async (ClaimsPrincipal u, Guid id, IMessagingService s, CancellationToken ct) =>
                    TypedResults.Ok(await s.MessagesAsync(User(u), id, ct))
            )
            .WithSummary("Read the latest 200 messages, participants only");
        threads
            .MapPost(
                "/{id:guid}/messages",
                async (
                    ClaimsPrincipal u,
                    Guid id,
                    SendMessageRequest r,
                    IMessagingService s,
                    CancellationToken ct
                ) =>
                {
                    var m = await s.SendAsync(User(u), id, r, ct);
                    return TypedResults.Created($"/api/threads/{id}/messages", m);
                }
            )
            .WithSummary("Send an asynchronous reply");
        api.MapPost(
                "/uploads",
                async (HttpRequest request, IImageService images, CancellationToken ct) =>
                {
                    Require(request.HasFormContentType, "Choose an image file.");
                    var form = await request.ReadFormAsync(ct);
                    var uploaded = await images.UploadAsync(form.Files.GetFile("file"), ct);
                    return TypedResults.Created(uploaded.Url, uploaded);
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithSummary("Upload a profile or gig image up to 5 MB");
    }
}
