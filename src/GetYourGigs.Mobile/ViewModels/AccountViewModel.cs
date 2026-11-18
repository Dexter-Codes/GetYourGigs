namespace GetYourGigs.Mobile.ViewModels;

public partial class AccountViewModel(
    GigsApiClient api,
    AppSession session,
    INavigator nav,
    IServerConfiguration serverConfiguration
) : ViewModelBase
{
    [ObservableProperty]
    private string name = session.Account?.Name ?? "";

    [ObservableProperty]
    private string apiAddress = api.BaseAddress?.ToString() ?? "";
    public string Email => session.Account?.Email ?? "Not signed in";
    public string Context => session.Account?.PreferredMode.ToString() ?? "Preview";

    [RelayCommand]
    private Task ProfileAsync() => nav.ProfileAsync();

    [RelayCommand]
    private Task SignOutAsync() =>
        RunAsync(async () =>
        {
            await session.SignOutAsync();
            await nav.BackAsync();
        });

    [RelayCommand]
    private Task SaveAsync() =>
        RunAsync(async () =>
        {
            var request = new UpdateAccountRequest(Name);
            Validate(request);
            await api.SendAsync<AccountResponse>("api/me/", request, "PUT");
            await session.RefreshAsync();
            await nav.BackAsync();
        });

    [RelayCommand]
    private Task ConfigureAsync() =>
        RunAsync(async () =>
        {
            await serverConfiguration.ChangeAsync(ApiAddress);
            Error = "API address saved. Sign in again against this server.";
        });
}
