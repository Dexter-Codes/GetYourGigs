namespace GetYourGigs.Mobile.ViewModels;

public partial class AuthViewModel(GigsApiClient api, AppSession session, Navigator nav)
    : ViewModelBase
{
    [ObservableProperty]
    private string apiAddress = api.BaseAddress?.ToString() ?? "";

    [ObservableProperty]
    private bool googleAvailable;

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
            if (
                !Uri.TryCreate(ApiAddress, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https")
            )
                throw new ArgumentException("Enter an absolute server address.");
#if !DEBUG
            if (uri.Scheme != "https")
                throw new ArgumentException("Use HTTPS for a production server.");
#endif
            await session.SignOutAsync();
            api.BaseAddress = uri;
            Preferences.Default.Set("api-address", uri.ToString());
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
            var verifier = Convert
                .ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            var challenge = Convert
                .ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.ASCII.GetBytes(verifier)
                    )
                )
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(api.BaseAddress!, "?mobileChallenge=" + challenge),
                new Uri("getyourgigs://auth")
            );
            if (!result.Properties.TryGetValue("code", out var code))
                throw new ArgumentException("Mobile sign-in did not return a code.");
            var auth = await api.SendAsync<AuthResponse>(
                "api/auth/mobile/exchange",
                new MobileExchangeRequest(code, verifier)
            );
            await FinishAsync(auth!);
        });

    [RelayCommand]
    private Task RequestCodeAsync() =>
        RunAsync(async () =>
        {
            var r = new EmailCodeRequest(Email);
            Validate(r);
            var result = await api.SendAsync<EmailCodeResponse>("api/auth/email/code", r);
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
            var r = new VerifyEmailRequest(Email, Code, Name);
            Validate(r);
            var auth = await api.SendAsync<AuthResponse>("api/auth/email/verify", r);
            await FinishAsync(auth!);
        });
}

public partial class ProfileEditViewModel(GigsApiClient api, AppSession session, Navigator nav)
    : MediaEditorViewModel(api)
{
    [ObservableProperty]
    private string headline = "";

    [ObservableProperty]
    private string category = "";

    [ObservableProperty]
    private string city = "";

    [ObservableProperty]
    private string rate = "0";

    [ObservableProperty]
    private string bio = "";

    [ObservableProperty]
    private string skills = "";

    [ObservableProperty]
    private bool available = true;
    public string[] CategoryOptions => Categories.All;

    [ObservableProperty]
    private string rateUnit = "per gig";
    public string[] RateUnits => ["per gig", "per hour", "per day", "per project"];

    public Task InitializeAsync() =>
        RunAsync(async () =>
        {
            var p = await Api.GetAsync<ProfileResponse>("api/me/profile");
            if (p is null)
                return;
            Headline = p.Headline;
            Category = p.Category;
            City = p.City;
            Rate = p.Rate.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Bio = p.Bio;
            Skills = string.Join(", ", p.Skills);
            Available = p.Available;
            Photos = new(p.Photos);
            RateUnit = p.RateUnit;
        });

    [RelayCommand]
    private Task SaveAsync() =>
        RunAsync(async () =>
        {
            if (!decimal.TryParse(Rate, out var rate))
                throw new ArgumentException("Enter a valid rate.");
            var r = new SaveProfileRequest(
                Headline,
                Category,
                City,
                rate,
                Bio,
                Skills.Split(
                    ',',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                ),
                Photos.ToArray(),
                RateUnit,
                Available
            );
            Validate(r);
            await Api.SendAsync<ProfileResponse>("api/me/profile", r, "PUT");
            await session.RefreshAsync();
            await nav.BackAsync();
        });
}

public partial class GigEditViewModel(GigsApiClient api, Navigator nav) : MediaEditorViewModel(api)
{
    [ObservableProperty]
    private int step;

    [ObservableProperty]
    private string title = "";

    [ObservableProperty]
    private string category = "";

    [ObservableProperty]
    private string city = "";

    [ObservableProperty]
    private string budget = "0";

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private DateTime date = DateTime.Today;

    [ObservableProperty]
    private bool hasDate;
    private Guid? id;
    public string[] CategoryOptions => Categories.All;
    public bool StepOne => Step == 0;
    public bool StepTwo => Step == 1;
    public bool StepThree => Step == 2;
    public string StepLabel => $"Step {Step + 1} of 3";
    public string NextLabel => Step == 2 ? "Post gig" : "Next";

    partial void OnStepChanged(int value)
    {
        OnPropertyChanged(nameof(StepOne));
        OnPropertyChanged(nameof(StepTwo));
        OnPropertyChanged(nameof(StepThree));
        OnPropertyChanged(nameof(StepLabel));
        OnPropertyChanged(nameof(NextLabel));
    }

    public void Initialize(GigResponse? g)
    {
        id = g?.Id;
        if (g is null)
            return;
        Title = g.Title;
        Category = g.Category;
        City = g.City;
        Budget = g.Budget.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Description = g.Description;
        HasDate = g.Date is not null;
        Date = g.Date?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
        Photos = new(g.Photos);
    }

    [RelayCommand]
    private Task NextAsync() =>
        RunAsync(async () =>
        {
            if (Step == 0)
            {
                if (Title.Trim().Length < 5 || !Categories.All.Contains(Category))
                    throw new ArgumentException(
                        "Add a title of at least five characters and choose a category."
                    );
                Step++;
                return;
            }
            if (!decimal.TryParse(Budget, out var budget))
                throw new ArgumentException("Enter a valid budget.");
            var r = new SaveGigRequest(
                Title,
                Category,
                City,
                Description,
                HasDate ? DateOnly.FromDateTime(Date) : null,
                budget,
                Photos.ToArray()
            );
            Validate(r);
            if (Step == 1)
            {
                Step++;
                return;
            }
            await Api.SendAsync<GigResponse>(
                "api/gigs/" + (id?.ToString() ?? ""),
                r,
                id is null ? "POST" : "PUT"
            );
            await nav.BackAsync();
        });

    [RelayCommand]
    private void Back()
    {
        if (Step > 0)
            Step--;
    }
}

public partial class ComposeViewModel(GigsApiClient api, Navigator nav) : ViewModelBase
{
    [ObservableProperty]
    private string heading = "";

    [ObservableProperty]
    private string subtitle = "";

    [ObservableProperty]
    private string text = "";

    [ObservableProperty]
    private string price = "";

    [ObservableProperty]
    private int rating = 5;

    [ObservableProperty]
    private bool isApplication;

    [ObservableProperty]
    private bool isReview;
    public int[] Ratings => [1, 2, 3, 4, 5];
    private string kind = "message";
    private Guid recipient;
    private Guid? gig;
    private Guid? application;

    public void Initialize(string kind, Guid recipient, Guid? gig, string title, Guid? application)
    {
        this.kind = kind;
        this.recipient = recipient;
        this.gig = gig;
        this.application = application;
        IsApplication = kind == "apply";
        IsReview = kind == "review";
        Heading =
            IsApplication ? "Why are you a great fit?"
            : IsReview ? "Good work deserves good words."
            : "Start with a little hello.";
        Subtitle = title;
    }

    [RelayCommand]
    private Task SendAsync() =>
        RunAsync(async () =>
        {
            if (kind == "apply")
            {
                decimal? price = null;
                if (!string.IsNullOrWhiteSpace(Price))
                {
                    if (!decimal.TryParse(Price, out var parsed))
                        throw new ArgumentException("Enter a valid quote.");
                    price = parsed;
                }
                var r = new ApplyRequest(Text, price);
                Validate(r);
                await api.SendAsync<ApplicationResponse>($"api/gigs/{gig}/applications", r);
                await nav.BackAsync();
            }
            else if (kind == "review")
            {
                var r = new CreateReviewRequest(application!.Value, Rating, Text);
                Validate(r);
                await api.SendAsync<ReviewResponse>("api/reviews", r);
                await nav.BackAsync();
            }
            else
            {
                var r = new StartThreadRequest(recipient, gig, Text);
                Validate(r);
                var thread = await api.SendAsync<ThreadResponse>("api/threads", r);
                await nav.BackAsync();
                await nav.ThreadAsync(thread!);
            }
        });
}
