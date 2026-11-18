using GetYourGigs.Client;

namespace GetYourGigs.Mobile.Services;

public sealed class SecureTokenStore : ITokenStore
{
    public async Task<string?> GetAsync() =>
        await SecureStorage.Default.GetAsync("gyg-access-token");

    public async Task SetAsync(string? token)
    {
        if (token is null)
            SecureStorage.Default.Remove("gyg-access-token");
        else
            await SecureStorage.Default.SetAsync("gyg-access-token", token);
    }
}
