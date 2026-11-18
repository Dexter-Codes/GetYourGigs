using GetYourGigs.Contracts;

namespace GetYourGigs.Api.Services;

public interface IBookingService
{
    Task<ApplicationResponse> ApplicationAsync(Guid user, Guid id, CancellationToken ct);
    Task<ReviewResponse> ReviewByIdAsync(Guid id, CancellationToken ct);
    Task<AllowanceResponse> AllowanceAsync(Guid user, CancellationToken ct);
    Task<ApplicationResponse> ApplyAsync(
        Guid user,
        Guid gigId,
        ApplyRequest request,
        CancellationToken ct
    );
    Task<ApplicationResponse[]> ApplicationsAsync(Guid user, Guid? gigId, CancellationToken ct);
    Task<ApplicationResponse> ChangeAsync(
        Guid user,
        Guid id,
        ApplicationActionRequest request,
        CancellationToken ct
    );
    Task<ApplicationResponse> CancelAsync(Guid user, Guid id, CancellationToken ct);
    Task<GigResponse> CompleteAsync(Guid user, Guid id, CancellationToken ct);
    Task<ReviewResponse> ReviewAsync(Guid user, CreateReviewRequest request, CancellationToken ct);
    Task<ReviewResponse[]> ReviewsAsync(Guid profileId, CancellationToken ct);
}
