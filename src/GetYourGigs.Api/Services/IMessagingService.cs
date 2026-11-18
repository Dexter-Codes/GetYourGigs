using GetYourGigs.Contracts;

namespace GetYourGigs.Api.Services;

public interface IMessagingService
{
    Task<ThreadResponse[]> ThreadsAsync(Guid user, CancellationToken ct);
    Task<ThreadResponse> StartAsync(Guid user, StartThreadRequest request, CancellationToken ct);
    Task<MessageResponse[]> MessagesAsync(Guid user, Guid thread, CancellationToken ct);
    Task<MessageResponse> SendAsync(
        Guid user,
        Guid thread,
        SendMessageRequest request,
        CancellationToken ct
    );
    Task<UpdateResponse[]> UpdatesAsync(Guid user, CancellationToken ct);
    Task ReadUpdatesAsync(Guid user, CancellationToken ct);
}
