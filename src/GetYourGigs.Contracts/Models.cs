using System.ComponentModel.DataAnnotations;

namespace GetYourGigs.Contracts;

/// <summary>Both contexts are available to every account.</summary>
public enum AccountMode
{
    Hiring,
    Working,
}

/// <summary>A gig's lifecycle.</summary>
public enum GigStatus
{
    Open,
    Booked,
    Completed,
    Closed,
}

/// <summary>A talent application's lifecycle.</summary>
public enum ApplicationStatus
{
    Applied,
    Shortlisted,
    Booked,
    Waitlisted,
    Declined,
    Cancelled,
    Completed,
    NotSelected,
}

/// <summary>The marketplace's disciplines.</summary>
public static class Categories
{
    public static readonly string[] All =
    [
        "Music & Performance",
        "Home & Trade Services",
        "Creative & Design",
        "Tech & Development",
        "Events & Hospitality",
        "Coaching & Wellness",
        "Business & Admin",
        "Other",
    ];
}

/// <summary>A signed-in account, independent of its active context.</summary>
public sealed record AccountResponse(
    Guid Id,
    string Name,
    string Email,
    AccountMode PreferredMode,
    bool HasProfile
);

/// <summary>A short-lived bearer token and current account.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AccountResponse Account
);

/// <summary>Starts email ownership verification.</summary>
public sealed record EmailCodeRequest([Required, EmailAddress, MaxLength(254)] string Email);

/// <summary>Delivery status; DevelopmentCode is only available in Development.</summary>
public sealed record EmailCodeResponse(string Message, string? DevelopmentCode);

/// <summary>Verifies an email code and creates or signs into the same account.</summary>
public sealed record VerifyEmailRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, RegularExpression("^[0-9]{6}$")] string Code,
    [Required, MinLength(2), MaxLength(80)] string Name
);

/// <summary>A Google token validated by the server.</summary>
public sealed record GoogleSignInRequest([Required] string IdToken);

/// <summary>Persists the account's marketplace context.</summary>
public sealed record UpdateModeRequest(AccountMode Mode);

/// <summary>Updates the account name.</summary>
public sealed record UpdateAccountRequest([Required, MinLength(2), MaxLength(80)] string Name);

/// <summary>The five-question onboarding and editable talent listing.</summary>
public sealed record SaveProfileRequest(
    [Required, MinLength(3), MaxLength(100)] string Headline,
    [Required, MaxLength(80)] string Category,
    [Required, MinLength(2), MaxLength(80)] string City,
    [Range(0, 10000000)] decimal Rate,
    [Required, MinLength(20), MaxLength(3000)] string Bio,
    string[]? Skills = null,
    string[]? Photos = null,
    string RateUnit = "per gig",
    bool Available = true
);

/// <summary>A public listing with completed-gig review statistics.</summary>
public sealed record ProfileResponse(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Headline,
    string Category,
    string City,
    decimal Rate,
    string RateUnit,
    string Bio,
    string[] Skills,
    string[] Photos,
    bool Available,
    double? Rating,
    int ReviewCount
);

/// <summary>A gig with a calendar date rather than ambiguous free text.</summary>
public sealed record SaveGigRequest(
    [Required, MinLength(5), MaxLength(120)] string Title,
    [Required, MaxLength(80)] string Category,
    [Required, MinLength(2), MaxLength(80)] string City,
    [Required, MinLength(20), MaxLength(4000)] string Description,
    DateOnly? Date,
    [Range(0, 10000000)] decimal Budget,
    string[]? Photos = null
);

/// <summary>A public gig and its owner identity.</summary>
public sealed record GigResponse(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string Title,
    string Category,
    string City,
    string Description,
    DateOnly? Date,
    decimal Budget,
    GigStatus Status,
    string[] Photos,
    int ApplicationCount,
    DateTimeOffset CreatedAt
);

/// <summary>A short application note and optional quote.</summary>
public sealed record ApplyRequest(
    [Required, MinLength(20), MaxLength(2000)] string Message,
    [Range(0, 10000000)] decimal? ExpectedPrice
);

/// <summary>An application visible to its applicant and hirer.</summary>
public sealed record ApplicationResponse(
    Guid Id,
    Guid GigId,
    string GigTitle,
    DateOnly? Date,
    Guid ApplicantId,
    string ApplicantName,
    Guid? ProfileId,
    string Message,
    decimal? ExpectedPrice,
    ApplicationStatus Status,
    DateTimeOffset CreatedAt
);

/// <summary>A hirer's requested application transition.</summary>
public sealed record ApplicationActionRequest(ApplicationStatus Status);

/// <summary>A saved listing.</summary>
public sealed record BookmarkResponse(Guid Id, string TargetType, Guid TargetId);

/// <summary>Saves a talent or gig.</summary>
public sealed record SaveBookmarkRequest([Required] string TargetType, Guid TargetId);

/// <summary>Starts a private conversation from a listing.</summary>
public sealed record StartThreadRequest(
    Guid RecipientId,
    Guid? GigId,
    [Required, MinLength(1), MaxLength(3000)] string Message
);

/// <summary>A message from a thread participant.</summary>
public sealed record SendMessageRequest([Required, MinLength(1), MaxLength(3000)] string Text);

/// <summary>A private message.</summary>
public sealed record MessageResponse(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string Text,
    DateTimeOffset CreatedAt
);

/// <summary>A conversation summary for its participant.</summary>
public sealed record ThreadResponse(
    Guid Id,
    Guid OtherUserId,
    string OtherName,
    Guid? GigId,
    string? GigTitle,
    string LastMessage,
    DateTimeOffset UpdatedAt,
    int UnreadCount
);

/// <summary>An account activity event.</summary>
public sealed record UpdateResponse(
    Guid Id,
    string Kind,
    string Text,
    Guid? GigId,
    DateTimeOffset CreatedAt,
    bool Read
);

/// <summary>A review from the hirer of a completed gig.</summary>
public sealed record CreateReviewRequest(
    Guid ApplicationId,
    [Range(1, 5)] int Rating,
    [Required, MinLength(10), MaxLength(1500)] string Text
);

/// <summary>A review tied to a completed application.</summary>
public sealed record ReviewResponse(
    Guid Id,
    string HirerName,
    string GigTitle,
    int Rating,
    string Text,
    DateTimeOffset CreatedAt
);

/// <summary>Pagination and anonymous preview metadata.</summary>
public sealed record PageResponse<T>(T[] Items, int Total, bool IsPreview, int Page, int PageSize);

/// <summary>The server-enforced daily application allowance in India Standard Time.</summary>
public sealed record AllowanceResponse(int Used, int Limit, DateOnly Day);

/// <summary>Deployment sign-in capabilities.</summary>
public sealed record PublicConfigResponse(
    string? GoogleClientId,
    bool DevelopmentEmail,
    string[] Categories
);

/// <summary>An uploaded media resource.</summary>
public sealed record UploadResponse(string Url);

/// <summary>Creates a one-time mobile handoff bound to a SHA-256 proof-key challenge.</summary>
public sealed record MobileGrantRequest(
    [Required, RegularExpression("^[A-Za-z0-9_-]{43}$")] string Challenge
);

/// <summary>A one-minute, single-use code; never a bearer token in a callback URL.</summary>
public sealed record MobileGrantResponse(string Code);

/// <summary>The native app redeems the handoff using its original proof-key verifier.</summary>
public sealed record MobileExchangeRequest(
    [Required, RegularExpression("^[A-Za-z0-9_-]{43}$")] string Code,
    [Required, RegularExpression("^[A-Za-z0-9_-]{43,128}$")] string Verifier
);
