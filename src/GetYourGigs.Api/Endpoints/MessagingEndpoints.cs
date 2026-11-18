using System.Security.Claims;
using GetYourGigs.Api.Services;
using GetYourGigs.Contracts;
using static GetYourGigs.Api.Endpoints.CurrentAccount;

namespace GetYourGigs.Api.Endpoints;

public static class MessagingEndpoints
{
    public static void MapMessaging(this RouteGroupBuilder api)
    {
        var threads = api.MapGroup("/threads").RequireAuthorization();
        threads
            .MapGet(
                "/",
                async (ClaimsPrincipal user, IMessagingService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.ThreadsAsync(User(user), ct))
            )
            .WithSummary("Get your private inbox");
        threads
            .MapPost(
                "/",
                async (
                    ClaimsPrincipal user,
                    StartThreadRequest request,
                    IMessagingService service,
                    CancellationToken ct
                ) =>
                {
                    var t = await service.StartAsync(User(user), request, ct);
                    return TypedResults.Created($"/api/threads/{t.Id}/messages", t);
                }
            )
            .WithSummary("Start or resume a conversation");
        threads
            .MapGet(
                "/{id:guid}/messages",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    IMessagingService service,
                    CancellationToken ct
                ) => TypedResults.Ok(await service.MessagesAsync(User(user), id, ct))
            )
            .WithSummary("Read the latest 200 messages, participants only");
        threads
            .MapPost(
                "/{id:guid}/messages",
                async (
                    ClaimsPrincipal user,
                    Guid id,
                    SendMessageRequest request,
                    IMessagingService service,
                    CancellationToken ct
                ) =>
                {
                    var m = await service.SendAsync(User(user), id, request, ct);
                    return TypedResults.Created($"/api/threads/{id}/messages", m);
                }
            )
            .WithSummary("Send an asynchronous reply");
    }
}
