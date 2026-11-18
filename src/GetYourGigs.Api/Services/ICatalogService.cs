using GetYourGigs.Contracts;

namespace GetYourGigs.Api.Services;

public interface ICatalogService
{
    Task<PageResponse<ProfileResponse>> ProfilesAsync(
        bool signedIn,
        string? q,
        string? category,
        string? city,
        bool? available,
        int page,
        CancellationToken ct
    );
    Task<ProfileResponse> ProfileAsync(Guid id, CancellationToken ct);
    Task<ProfileResponse?> MyProfileAsync(Guid user, CancellationToken ct);
    Task<ProfileResponse> SaveProfileAsync(
        Guid user,
        SaveProfileRequest request,
        CancellationToken ct
    );
    Task<PageResponse<GigResponse>> GigsAsync(
        bool signedIn,
        string? q,
        string? category,
        string? city,
        int page,
        CancellationToken ct
    );
    Task<GigResponse> GigAsync(Guid id, CancellationToken ct);
    Task<GigResponse[]> MineAsync(Guid user, CancellationToken ct);
    Task<GigResponse> SaveGigAsync(
        Guid user,
        Guid? id,
        SaveGigRequest request,
        CancellationToken ct
    );
    Task<GigResponse> CloseAsync(Guid user, Guid id, CancellationToken ct);
    Task<BookmarkResponse> BookmarkByIdAsync(Guid user, Guid id, CancellationToken ct);
    Task<BookmarkResponse[]> BookmarksAsync(Guid user, CancellationToken ct);
    Task<BookmarkResponse> BookmarkAsync(
        Guid user,
        SaveBookmarkRequest request,
        CancellationToken ct
    );
    Task RemoveBookmarkAsync(Guid user, Guid id, CancellationToken ct);
}
