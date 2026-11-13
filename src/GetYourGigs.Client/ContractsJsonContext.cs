using System.Text.Json.Serialization;
using GetYourGigs.Contracts;

namespace GetYourGigs.Client;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(MobileGrantRequest))]
[JsonSerializable(typeof(MobileGrantResponse))]
[JsonSerializable(typeof(MobileExchangeRequest))]
[JsonSerializable(typeof(AccountResponse))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(PublicConfigResponse))]
[JsonSerializable(typeof(EmailCodeRequest))]
[JsonSerializable(typeof(EmailCodeResponse))]
[JsonSerializable(typeof(VerifyEmailRequest))]
[JsonSerializable(typeof(GoogleSignInRequest))]
[JsonSerializable(typeof(UpdateModeRequest))]
[JsonSerializable(typeof(UpdateAccountRequest))]
[JsonSerializable(typeof(ProfileResponse))]
[JsonSerializable(typeof(ProfileResponse[]))]
[JsonSerializable(typeof(SaveProfileRequest))]
[JsonSerializable(typeof(PageResponse<ProfileResponse>))]
[JsonSerializable(typeof(GigResponse))]
[JsonSerializable(typeof(GigResponse[]))]
[JsonSerializable(typeof(PageResponse<GigResponse>))]
[JsonSerializable(typeof(SaveGigRequest))]
[JsonSerializable(typeof(ApplicationResponse))]
[JsonSerializable(typeof(ApplicationResponse[]))]
[JsonSerializable(typeof(ApplyRequest))]
[JsonSerializable(typeof(ApplicationActionRequest))]
[JsonSerializable(typeof(AllowanceResponse))]
[JsonSerializable(typeof(BookmarkResponse))]
[JsonSerializable(typeof(BookmarkResponse[]))]
[JsonSerializable(typeof(SaveBookmarkRequest))]
[JsonSerializable(typeof(ThreadResponse))]
[JsonSerializable(typeof(ThreadResponse[]))]
[JsonSerializable(typeof(MessageResponse))]
[JsonSerializable(typeof(MessageResponse[]))]
[JsonSerializable(typeof(StartThreadRequest))]
[JsonSerializable(typeof(SendMessageRequest))]
[JsonSerializable(typeof(UpdateResponse[]))]
[JsonSerializable(typeof(ReviewResponse))]
[JsonSerializable(typeof(ReviewResponse[]))]
[JsonSerializable(typeof(CreateReviewRequest))]
[JsonSerializable(typeof(UploadResponse))]
internal partial class ContractsJsonContext : JsonSerializerContext { }
