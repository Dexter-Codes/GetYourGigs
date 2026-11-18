namespace GetYourGigs.Mobile.ViewModels;

public partial class AuthViewModel(
    GigsApiClient api,
    AppSession session,
    INavigator nav,
    IServerConfiguration serverConfiguration,
    IBrowserSignIn browserSignIn
) : ViewModelBase
{
    [ObservableProperty]
    private string apiAddress = api.BaseAddress?.ToString() ?? "";

    [ObservableProperty]
    private bool googleAvailable;

    [RelayCommand]
    public Task InitializeAsync() =>
        RunAsync(async () =>
        {
            var config = await api.GetAsync<PublicConfigResponse>("api/config");
            GoogleAvailable = !string.IsNullOrWhiteSpace(config?.GoogleClientId);
        });

    [RelayCommand]
    private Task ConfigureServerAsync() =>
        RunAsync(async () =>
        {
            await serverConfiguration.ChangeAsync(ApiAddress);
            var config = await api.GetAsync<PublicConfigResponse>("api/config");
            GoogleAvailable = !string.IsNullOrWhiteSpace(config?.GoogleClientId);
            Error = "API address saved.";
        });

    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string code = "";

    [ObservableProperty]
    private bool codeSent;

    [ObservableProperty]
    private string developmentNotice = "";

    [RelayCommand]
    private Task BrowserSignInAsync() =>
        RunAsync(async () =>
        {
            var auth = await browserSignIn.SignInAsync();
            await FinishAsync(auth!);
        });

    [RelayCommand]
    private Task RequestCodeAsync() =>
        RunAsync(async () =>
        {
            var request = new EmailCodeRequest(Email);
            Validate(request);
            var result = await api.SendAsync<EmailCodeResponse>("api/auth/email/code", request);
            CodeSent = true;
            DevelopmentNotice = result?.DevelopmentCode is null
                ? "Check your email. Your code expires in 10 minutes."
                : $"LOCAL DEVELOPMENT: no email was sent. Test code: {result.DevelopmentCode}.";
        });

    private async Task FinishAsync(AuthResponse auth)
    {
        await session.SignInAsync(auth);
        if (!auth.Account.HasProfile && session.PreviewMode == AccountMode.Working)
            await session.ChangeModeAsync(AccountMode.Working);
        await nav.BackAsync();
        if (!auth.Account.HasProfile && session.Account!.PreferredMode == AccountMode.Working)
            await nav.ProfileAsync();
    }

    [RelayCommand]
    private Task VerifyAsync() =>
        RunAsync(async () =>
        {
            var request = new VerifyEmailRequest(Email, Code, Name);
            Validate(request);
            var auth = await api.SendAsync<AuthResponse>("api/auth/email/verify", request);
            await FinishAsync(auth!);
        });
}
