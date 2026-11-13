using System.Globalization;

namespace GetYourGigs.Mobile.Models;

public sealed record ListingItem(
    string Id,
    string Kind,
    string Title,
    string Subtitle,
    string Photo,
    string Meta,
    string Price,
    string Status,
    bool Saved,
    object Value
)
{
    public string SampleLabel =>
        Value is ProfileResponse p && p.Name.Contains("(sample)", StringComparison.Ordinal)
        || Value is GigResponse g && g.Title.Contains("(sample)", StringComparison.Ordinal)
            ? "Sample listing · demonstration content"
            : "";
    public bool CanSave => Kind is "profile" or "gig";
    public string SaveIcon => Saved ? "nav_heart_active.png" : "nav_heart.png";
    public string SaveDescription => Saved ? "Remove saved listing" : "Save listing";
    public string Initial => Title.Length == 0 ? "G" : Title[..1];
    public bool HasPhoto => !string.IsNullOrWhiteSpace(Photo);

    public static string Money(decimal value) =>
        value == 0 ? "Let’s discuss" : value.ToString("C0", CultureInfo.GetCultureInfo("en-IN"));

    public static string Date(DateOnly? date) =>
        date?.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) ?? "Date to be agreed";

    public static ListingItem From(ProfileResponse p, bool saved = false) =>
        new(
            p.Id.ToString(),
            "profile",
            p.Name.Replace(" (sample)", ""),
            p.Headline,
            p.Photos.FirstOrDefault() ?? "",
            p.City + " · " + p.Category,
            Money(p.Rate) + " / " + p.RateUnit.Replace("per ", ""),
            p.Available ? "Available" : "Currently booked",
            saved,
            p
        );

    public static ListingItem From(GigResponse g, bool saved = false) =>
        new(
            g.Id.ToString(),
            "gig",
            g.Title.Replace(" (sample)", ""),
            g.Category,
            g.Photos.FirstOrDefault() ?? "",
            g.City + " · " + Date(g.Date),
            Money(g.Budget),
            g.Status.ToString(),
            saved,
            g
        );

    public static ListingItem From(ApplicationResponse a) =>
        new(
            a.Id.ToString(),
            "application",
            a.GigTitle.Replace(" (sample)", ""),
            a.Message,
            "",
            Date(a.Date),
            a.ExpectedPrice is null ? "Quote to discuss" : Money(a.ExpectedPrice.Value),
            a.Status.ToString(),
            false,
            a
        );

    public static ListingItem From(ThreadResponse t) =>
        new(
            t.Id.ToString(),
            "thread",
            t.OtherName.Replace(" (sample)", ""),
            t.LastMessage,
            "",
            t.GigTitle ?? "A new connection",
            "",
            t.UnreadCount > 0 ? $"{t.UnreadCount} new" : "Open conversation",
            false,
            t
        );

    public static ListingItem From(UpdateResponse a) =>
        new(
            a.Id.ToString(),
            "update",
            a.Kind.ToUpperInvariant(),
            a.Text,
            "",
            a.CreatedAt.ToLocalTime().ToString("dd MMM, HH:mm"),
            "",
            a.Read ? "Read" : "New",
            false,
            a
        );
}

public sealed record CategoryItem(string Title, string Icon)
{
    // Short labels fit the mobile tiles; Title remains the API filter value.
    public string DisplayTitle =>
        Title switch
        {
            "Music & Performance" => "Music",
            "Home & Trade Services" => "Home",
            "Creative & Design" => "Design",
            "Tech & Development" => "Tech",
            "Events & Hospitality" => "Events",
            "Coaching & Wellness" => "Wellness",
            "Business & Admin" => "Business",
            _ => Title,
        };
}
