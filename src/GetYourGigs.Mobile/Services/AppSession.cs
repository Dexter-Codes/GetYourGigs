namespace GetYourGigs.Mobile.Services;

public sealed class AppSession(GigsApiClient api, ITokenStore tokens)
{
    public AccountResponse? Account { get; private set; }
    public event EventHandler? Changed;
    public bool SignedIn => Account is not null;
    public AccountMode PreviewMode { get; set; }

    public async Task RestoreAsync()
    {
        if (await tokens.GetAsync() is null)
            return;
        try
        {
            Account = await api.GetAsync<AccountResponse>("api/me/");
        }
        catch (ApiRequestException e) when (e.Status == System.Net.HttpStatusCode.Unauthorized)
        {
            await tokens.SetAsync(null);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SignInAsync(AuthResponse auth)
    {
        await tokens.SetAsync(auth.AccessToken);
        Account = auth.Account;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshAsync()
    {
        Account = await api.GetAsync<AccountResponse>("api/me/");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task ChangeModeAsync(AccountMode mode)
    {
        Account = await api.SendAsync<AccountResponse>(
            "api/me/mode",
            new UpdateModeRequest(mode),
            "PUT"
        );
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SignOutAsync()
    {
        await tokens.SetAsync(null);
        Account = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
