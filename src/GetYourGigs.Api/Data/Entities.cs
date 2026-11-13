using GetYourGigs.Contracts;

namespace GetYourGigs.Api.Data;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public AccountMode PreferredMode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public TalentProfile? Profile { get; set; }
}

public sealed class TalentProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public Account Owner { get; set; } = null!;
    public string Headline { get; set; } = "";
    public string Category { get; set; } = "";
    public string City { get; set; } = "";
    public string Bio { get; set; } = "";
    public decimal Rate { get; set; }
    public string RateUnit { get; set; } = "per gig";
    public bool Available { get; set; } = true;
    public string[] Skills { get; set; } = [];
    public string[] Photos { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
}

public sealed class Gig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public Account Owner { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string City { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly? Date { get; set; }
    public decimal Budget { get; set; }
    public GigStatus Status { get; set; }
    public string[] Photos { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<GigApplication> Applications { get; set; } = [];
}

public sealed class GigApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GigId { get; set; }
    public Gig Gig { get; set; } = null!;
    public Guid ApplicantId { get; set; }
    public Account Applicant { get; set; } = null!;
    public string Message { get; set; } = "";
    public decimal? ExpectedPrice { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateOnly ApplicationDay { get; set; }
}

public sealed class Bookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string TargetType { get; set; } = "";
    public Guid TargetId { get; set; }
}

public sealed class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FirstId { get; set; }
    public Account First { get; set; } = null!;
    public Guid SecondId { get; set; }
    public Account Second { get; set; } = null!;
    public Guid? GigId { get; set; }
    public Gig? Gig { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Message> Messages { get; set; } = [];
}

public sealed class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Guid SenderId { get; set; }
    public Account Sender { get; set; } = null!;
    public string Text { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Read { get; set; }
}

public sealed class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string Kind { get; set; } = "";
    public string Text { get; set; } = "";
    public Guid? GigId { get; set; }
    public bool Read { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public GigApplication Application { get; set; } = null!;
    public Guid ProfileId { get; set; }
    public TalentProfile Profile { get; set; } = null!;
    public Guid HirerId { get; set; }
    public Account Hirer { get; set; } = null!;
    public int Rating { get; set; }
    public string Text { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EmailChallenge
{
    public string Email { get; set; } = "";
    public string Hash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public int Attempts { get; set; }
}

public sealed class MobileGrant
{
    public string Hash { get; set; } = "";
    public Guid AccountId { get; set; }
    public string Challenge { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}
