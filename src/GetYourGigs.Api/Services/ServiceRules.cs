using GetYourGigs.Contracts;

namespace GetYourGigs.Api.Services;

public sealed class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public static class ServiceRules
{
    public static void Require(bool condition, string message, int status = 400)
    {
        if (!condition)
            throw new ApiException(status, message);
    }

    public static T Found<T>(T? value)
        where T : class => value ?? throw new ApiException(404, "This item was not found.");

    public static void Own(bool condition) =>
        Require(condition, "You do not have access to this item.", 403);

    public static void Category(string category) =>
        Require(Categories.All.Contains(category), "Choose a supported category.");

    public static void Photos(string[]? photos)
    {
        Require(photos is null || photos.Length <= 5, "Use up to five photos.");
        foreach (var url in photos ?? [])
            Require(
                !string.IsNullOrWhiteSpace(url)
                    && (
                        url.StartsWith("/uploads/", StringComparison.Ordinal)
                        || (
                            Uri.TryCreate(url, UriKind.Absolute, out var uri)
                            && uri.Scheme == "https"
                        )
                    ),
                "Photos must use HTTPS or an uploaded image."
            );
    }

    public static readonly TimeZoneInfo MarketplaceZone = TimeZoneInfo.FindSystemTimeZoneById(
        "Asia/Kolkata"
    );

    public static DateOnly Today(TimeProvider time) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), MarketplaceZone).DateTime);
}
