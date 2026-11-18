using System.Collections.ObjectModel;

namespace GetYourGigs.Mobile.ViewModels;

public partial class DetailViewModel(
    GigsApiClient api,
    AppSession session,
    INavigator nav,
    IUserDialogs dialogs
) : ViewModelBase
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

    [RelayCommand]
    public Task LoadAsync() =>
        RunAsync(async () =>
        {
            if (Item?.Value is ApplicationResponse selectedApplication)
            {
                application = await api.GetAsync<ApplicationResponse>(
                    "api/applications/" + selectedApplication.Id
                );
                gig = await api.GetAsync<GigResponse>("api/gigs/" + selectedApplication.GigId);
            }
            else if (Item?.Value is GigResponse selectedGig)
                gig = await api.GetAsync<GigResponse>("api/gigs/" + selectedGig.Id);
            else if (Item?.Value is ProfileResponse selectedProfile)
                profile = await api.GetAsync<ProfileResponse>("api/profiles/" + selectedProfile.Id);
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
            if (
                !await dialogs.ConfirmAsync(
                    "Did the gig take place?",
                    "Confirm that the booked talent actually completed this gig. This enables a genuine review.",
                    "Confirm completed",
                    "Go back"
                )
            )
                return;
            gig = await api.SendAsync<GigResponse>($"api/gigs/{gig!.Id}/complete");
            ShowComplete = false;
            Status = "Completed";
        });

    [RelayCommand]
    public Task CloseAsync() =>
        RunAsync(async () =>
        {
            if (
                !await dialogs.ConfirmAsync(
                    "Close this gig?",
                    "New applications will stop, and current applicants will be notified.",
                    "Close gig",
                    "Go back"
                )
            )
                return;
            gig = await api.SendAsync<GigResponse>($"api/gigs/{gig!.Id}/close");
            ShowEdit = false;
            Status = "Closed";
        });

    [RelayCommand]
    public Task WithdrawAsync() =>
        RunAsync(async () =>
        {
            if (
                !await dialogs.ConfirmAsync(
                    "Withdraw from this gig?",
                    "The hirer will be notified. This does not restore today's application allowance.",
                    "Withdraw",
                    "Go back"
                )
            )
                return;
            await api.SendAsync<ApplicationResponse>(
                $"api/applications/{application!.Id}/withdraw"
            );
            ShowWithdraw = false;
            Status = "Cancelled";
        });
}
