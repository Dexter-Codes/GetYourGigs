using GetYourGigs.Contracts;

namespace GetYourGigs.Api.Services;

public interface IAuthService
{
    Task<MobileGrantResponse> MobileGrantAsync(
        Guid user,
        MobileGrantRequest request,
        CancellationToken ct
    );
    Task<AuthResponse> ExchangeAsync(MobileExchangeRequest request, CancellationToken ct);
    Task<EmailCodeResponse> RequestCodeAsync(EmailCodeRequest request, CancellationToken ct);
    Task<AuthResponse> VerifyAsync(VerifyEmailRequest request, CancellationToken ct);
    Task<AuthResponse> GoogleAsync(GoogleSignInRequest request, CancellationToken ct);
    Task<AccountResponse> AccountAsync(Guid id, CancellationToken ct);
    Task<AccountResponse> ModeAsync(Guid id, UpdateModeRequest request, CancellationToken ct);
    Task<AccountResponse> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken ct);
}
