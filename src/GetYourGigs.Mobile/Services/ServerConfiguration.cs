namespace GetYourGigs.Mobile.Services;

/// <summary>Validates and persists a server change after clearing the previous session.</summary>
public sealed class ServerConfiguration(GigsApiClient api, AppSession session)
    : IServerConfiguration
{
    public async Task ChangeAsync(string address)
    {
        if (
            !Uri.TryCreate(address, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
        )
            throw new ArgumentException(
                "Enter an absolute HTTP(S) server address without credentials, a query, or a fragment."
            );
#if !DEBUG
        if (uri.Scheme != "https")
            throw new ArgumentException("Production API addresses must use HTTPS.");
#endif
        await session.SignOutAsync();
        api.BaseAddress = new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
        Preferences.Default.Set("api-address", api.BaseAddress.ToString());
    }
}
