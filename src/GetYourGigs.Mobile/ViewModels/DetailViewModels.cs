using System.Collections.ObjectModel;

namespace GetYourGigs.Mobile.ViewModels;

public partial class DetailViewModel(GigsApiClient api, AppSession session, Navigator nav)
    : ViewModelBase
{
    [ObservableProperty]
    private ListingItem? item;

    [ObservableProperty]
    private string title = "";

    [ObservableProperty]
    private string category = "";

    [ObservableProperty]
    private string city = "";

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private string price = "";

    [ObservableProperty]
    private string photo = "";

    [ObservableProperty]
    private string status = "";

    [ObservableProperty]
    private string date = "";

    [ObservableProperty]
    private string reviews = "";

    [ObservableProperty]
    private bool isProfile;

    [ObservableProperty]
    private bool showApply;

    [ObservableProperty]
    private bool showApplicants;

    [ObservableProperty]
    private bool showEdit;

    [ObservableProperty]
    private bool showComplete;

    [ObservableProperty]
    private bool showWithdraw;

    [ObservableProperty]
    private bool showMessage;
    private GigResponse? gig;
    private ProfileResponse? profile;
    private ApplicationResponse? application;

    public Task LoadAsync() =>
        RunAsync(async () =>
        {
            if (Item?.Value is ApplicationResponse a)
            {
                application = a;
                gig = await api.GetAsync<GigResponse>("api/gigs/" + a.GigId);
            }
            else if (Item?.Value is GigResponse g)
                gig = await api.GetAsync<GigResponse>("api/gigs/" + g.Id);
            else if (Item?.Value is ProfileResponse p)
                profile = await api.GetAsync<ProfileResponse>("api/profiles/" + p.Id);
            IsProfile = profile is not null;
            if (profile is not null)
            {
                Title = profile.Name.Replace(" (sample)", "");
                Category = profile.Headline;
                City = profile.City;
                Description = profile.Bio + "\n\nSkills: " + string.Join(", ", profile.Skills);
                Price = ListingItem.Money(profile.Rate) + " " + profile.RateUnit;
                Photo = profile.Photos.FirstOrDefault() ?? "";
                Status = profile.Available ? "Available" : "Currently booked";
                ShowMessage = profile.OwnerId != session.Account?.Id;
                var reviews = await api.GetAsync<ReviewResponse[]>(
                    "api/profiles/" + profile.Id + "/reviews"
                );
                Reviews =
                    reviews?.Length > 0
                        ? string.Join(
                            "\n\n",
                            reviews.Select(x => $"{x.Rating}/5 · {x.HirerName}\n{x.Text}")
                        )
                        : "No completed-gig reviews yet. Good work will speak for itself.";
            }
            else if (gig is not null)
            {
                Title = gig.Title.Replace(" (sample)", "");
                Category = gig.Category;
                City = gig.City;
                Description = gig.Description;
                Price = ListingItem.Money(gig.Budget);
                Photo = gig.Photos.FirstOrDefault() ?? "";
                Status = application?.Status.ToString() ?? gig.Status.ToString();
                Date = ListingItem.Date(gig.Date);
                var own = gig.OwnerId == session.Account?.Id;
                ShowApplicants = own;
                ShowEdit = own && gig.Status == GigStatus.Open;
                ShowComplete = own && gig.Status == GigStatus.Booked;
                ShowMessage = !own;
                ShowApply = !own && application is null && gig.Status == GigStatus.Open;
                ShowWithdraw =
                    application?.Status
                        is ApplicationStatus.Applied
                            or ApplicationStatus.Shortlisted
                            or ApplicationStatus.Waitlisted
                            or ApplicationStatus.Booked;
            }
        });

    private async Task<bool> EnsureAccountAsync()
    {
        if (session.SignedIn)
            return true;
        await nav.SignInAsync();
        return false;
    }

    [RelayCommand]
    private Task ApplyAsync() =>
        RunAsync(async () =>
        {
            if (!await EnsureAccountAsync())
                return;
            if (!session.Account!.HasProfile)
            {
                await nav.ProfileAsync();
                return;
            }
            await nav.ComposeAsync("apply", gig!.OwnerId, gig.Id, gig.Title);
        });

    [RelayCommand]
    private Task MessageAsync() =>
        RunAsync(async () =>
        {
            if (!await EnsureAccountAsync())
                return;
            await nav.ComposeAsync(
                "message",
                profile?.OwnerId ?? gig!.OwnerId,
                gig?.Id,
                profile?.Name ?? gig!.OwnerName
            );
        });

    [RelayCommand]
    private Task SaveAsync() =>
        RunAsync(async () =>
        {
            if (!await EnsureAccountAsync())
                return;
            await api.SendAsync<BookmarkResponse>(
                "api/me/bookmarks",
                new SaveBookmarkRequest(IsProfile ? "profile" : "gig", profile?.Id ?? gig!.Id)
            );
            Status = "Saved for later";
        });

    [RelayCommand]
    private Task ApplicantsAsync() => nav.ApplicantsAsync(gig!);

    [RelayCommand]
    private Task EditAsync() => nav.PostAsync(gig);

    [RelayCommand]
    public Task CompleteAsync() =>
        RunAsync(async () =>
        {
            gig = await api.SendAsync<GigResponse>($"api/gigs/{gig!.Id}/complete");
            ShowComplete = false;
            Status = "Completed";
        });

    [RelayCommand]
    public Task CloseAsync() =>
        RunAsync(async () =>
        {
            gig = await api.SendAsync<GigResponse>($"api/gigs/{gig!.Id}/close");
            ShowEdit = false;
            Status = "Closed";
        });

    [RelayCommand]
    public Task WithdrawAsync() =>
        RunAsync(async () =>
        {
            await api.SendAsync<ApplicationResponse>(
                $"api/applications/{application!.Id}/withdraw"
            );
            ShowWithdraw = false;
            Status = "Cancelled";
        });
}

public partial class ApplicantsViewModel(GigsApiClient api, Navigator nav) : ViewModelBase
{
    public GigResponse? Gig { get; set; }

    [ObservableProperty]
    private ObservableCollection<ApplicationResponse> items = [];
    public string Title => Gig?.Title ?? "Meet your applicants";

    public Task LoadAsync() =>
        RunAsync(async () =>
        {
            Items = new(
                await api.GetAsync<ApplicationResponse[]>($"api/gigs/{Gig!.Id}/applications") ?? []
            );
            OnPropertyChanged(nameof(Title));
        });

    [RelayCommand]
    private Task ChangeAsync(string value) =>
        RunAsync(async () =>
        {
            var parts = value.Split(':');
            await api.SendAsync<ApplicationResponse>(
                $"api/applications/{parts[0]}/status",
                new ApplicationActionRequest(Enum.Parse<ApplicationStatus>(parts[1])),
                "PUT"
            );
            Items = new(
                await api.GetAsync<ApplicationResponse[]>($"api/gigs/{Gig!.Id}/applications") ?? []
            );
        });

    public Task ChangeAsync(ApplicationResponse app, ApplicationStatus status) =>
        RunAsync(async () =>
        {
            await api.SendAsync<ApplicationResponse>(
                $"api/applications/{app.Id}/status",
                new ApplicationActionRequest(status),
                "PUT"
            );
            Items = new(
                await api.GetAsync<ApplicationResponse[]>($"api/gigs/{Gig!.Id}/applications") ?? []
            );
        });

    [RelayCommand]
    private Task MessageAsync(ApplicationResponse app) =>
        nav.ComposeAsync("message", app.ApplicantId, app.GigId, app.ApplicantName);

    [RelayCommand]
    private Task ReviewAsync(ApplicationResponse app) =>
        RunAsync(async () =>
        {
            if (app.Status != ApplicationStatus.Completed)
                throw new ArgumentException("Reviews are available only after a gig is completed.");
            await nav.ComposeAsync("review", app.ApplicantId, app.GigId, app.GigTitle, app.Id);
        });
}

public sealed record ChatMessage(string Text, string Time, bool IsMine)
{
    public LayoutOptions Alignment => IsMine ? LayoutOptions.End : LayoutOptions.Start;
    public Color Background => Color.FromArgb(IsMine ? "#E5EFE7" : "#F5F4EF");
}

public partial class ThreadViewModel(GigsApiClient api, AppSession session) : ViewModelBase
{
    public ThreadResponse? Thread { get; set; }
    public string Heading => Thread?.OtherName.Replace(" (sample)", "") ?? "Conversation";
    public string Subtitle => Thread?.GigTitle ?? "Good things start with hello.";

    [ObservableProperty]
    private ObservableCollection<ChatMessage> messages = [];

    [ObservableProperty]
    private string text = "";

    public Task LoadAsync() =>
        RunAsync(async () =>
        {
            if (Thread is null)
                return;
            var messages =
                await api.GetAsync<MessageResponse[]>($"api/threads/{Thread.Id}/messages") ?? [];
            Messages = new(
                messages.Select(x => new ChatMessage(
                    x.Text,
                    x.CreatedAt.ToLocalTime().ToString("dd MMM · HH:mm"),
                    x.SenderId == session.Account?.Id
                ))
            );
            OnPropertyChanged(nameof(Heading));
            OnPropertyChanged(nameof(Subtitle));
        });

    [RelayCommand]
    private Task SendAsync() =>
        RunAsync(async () =>
        {
            if (Thread is null)
                return;
            var request = new SendMessageRequest(Text);
            Validate(request);
            await api.SendAsync<MessageResponse>($"api/threads/{Thread.Id}/messages", request);
            Text = "";
            var messages =
                await api.GetAsync<MessageResponse[]>($"api/threads/{Thread.Id}/messages") ?? [];
            Messages = new(
                messages.Select(x => new ChatMessage(
                    x.Text,
                    x.CreatedAt.ToLocalTime().ToString("HH:mm"),
                    x.SenderId == session.Account?.Id
                ))
            );
        });
}

public partial class AccountViewModel(GigsApiClient api, AppSession session, Navigator nav)
    : ViewModelBase
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
            if (
                !Uri.TryCreate(ApiAddress, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https")
            )
                throw new ArgumentException("Enter an absolute HTTP(S) API address.");
#if !DEBUG
            if (uri.Scheme != "https")
                throw new ArgumentException("Production API addresses must use HTTPS.");
#endif
            await session.SignOutAsync();
            api.BaseAddress = uri;
            Preferences.Default.Set("api-address", uri.ToString());
            Error = "API address saved. Sign in again against this server.";
        });
}
