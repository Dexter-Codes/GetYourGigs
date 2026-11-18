using System.Collections.ObjectModel;

namespace GetYourGigs.Mobile.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly GigsApiClient api;
    private readonly AppSession session;
    private readonly INavigator nav;
    private CancellationTokenSource? searchCancellation;

    public MainViewModel(GigsApiClient api, AppSession session, INavigator nav)
    {
        this.api = api;
        this.session = session;
        this.nav = nav;
        Categories = GetYourGigs
            .Contracts.Categories.All.Select(
                (x, i) =>
                    new CategoryItem(
                        x,
                        new[]
                        {
                            "category_music.png",
                            "category_home.png",
                            "category_design.png",
                            "category_tech.png",
                            "category_event.png",
                            "category_wellness.png",
                            "category_business.png",
                            "category_more.png",
                        }[i]
                    )
            )
            .ToArray();
    }

    private bool initialized;

    [RelayCommand]
    private async Task AppearAsync()
    {
        if (!initialized)
        {
            await InitializeAsync();
            initialized = true;
        }
        else
            await LoadAsync();
        OnPropertyChanged(nameof(SignedIn));
        OnPropertyChanged(nameof(IsHiring));
        OnPropertyChanged(nameof(ModeLabel));
        OnPropertyChanged(nameof(AccountLabel));
    }

    [ObservableProperty]
    private ObservableCollection<ListingItem> items = [];

    [ObservableProperty]
    private string search = "";

    [ObservableProperty]
    private string city = "";

    [ObservableProperty]
    private string selectedCategory = "";

    [ObservableProperty]
    private string tab = "Home";

    [ObservableProperty]
    private AccountMode guestMode;

    [ObservableProperty]
    private string resultLabel = "Finding your next connection…";

    [ObservableProperty]
    private bool showGate;

    [ObservableProperty]
    private bool empty;
    public CategoryItem[] Categories { get; }
    public bool SignedIn => session.SignedIn;
    public bool IsHiring => (session.Account?.PreferredMode ?? GuestMode) == AccountMode.Hiring;
    public string ModeLabel => IsHiring ? "HIRING" : "WORKING";
    public string AccountLabel => SignedIn ? session.Account!.Name.Split(' ')[0] : "Sign in";
    public bool ShowHero => Tab == "Home";
    public string Heading =>
        Tab switch
        {
            "Home" => IsHiring ? "Meet your people." : "Find your next gig.",
            "Gigs" => IsHiring ? "My posted gigs" : "My applications",
            "Saved" => "Saved for later",
            "Inbox" => "Your conversations",
            _ => "Your latest updates",
        };

    partial void OnSearchChanged(string value) => Debounce();

    partial void OnCityChanged(string value) => Debounce();

    private void Debounce()
    {
        searchCancellation?.Cancel();
        searchCancellation?.Dispose();
        var cancellation = searchCancellation = new CancellationTokenSource();
        _ = DebounceAsync(cancellation.Token);
    }

    private async Task DebounceAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(350, ct);
            await LoadAsync();
        }
        catch (OperationCanceledException) { }
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await RunAsync(async () =>
        {
            await session.RestoreAsync();
        });
        await LoadAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [ObservableProperty]
    private bool isRefreshing;

    private int pageNumber = 1;

    [ObservableProperty]
    private bool canLoadMore;

    [RelayCommand]
    private Task LoadMoreAsync() => LoadAsync(true);

    private readonly SemaphoreSlim loadGate = new(1, 1);
    private int loadVersion;

    public async Task LoadAsync(bool append = false)
    {
        var version = Interlocked.Increment(ref loadVersion);
        await loadGate.WaitAsync();
        try
        {
            if (version != loadVersion)
                return;
            while (IsBusy)
                await Task.Delay(30);
            await RunAsync(async () =>
            {
                if (!SignedIn && Tab != "Home")
                    Tab = "Home";
                pageNumber = append ? pageNumber + 1 : 1;
                CanLoadMore = false;
                var saved = SignedIn
                    ? await api.GetAsync<BookmarkResponse[]>("api/me/bookmarks") ?? []
                    : [];
                IEnumerable<ListingItem> items;
                if (Tab == "Home")
                {
                    var query =
                        $"?page={pageNumber}&q={Uri.EscapeDataString(Search)}&city={Uri.EscapeDataString(City)}&category={Uri.EscapeDataString(SelectedCategory)}";
                    if (IsHiring)
                    {
                        var page = await api.GetAsync<PageResponse<ProfileResponse>>(
                            "api/profiles" + query
                        );
                        items = page!.Items.Select(x =>
                            ListingItem.From(
                                x,
                                saved.Any(s => s.TargetType == "profile" && s.TargetId == x.Id)
                            )
                        );
                        CanLoadMore = SignedIn && pageNumber * page.PageSize < page.Total;
                        ResultLabel = $"{page.Total} talented people";
                    }
                    else
                    {
                        var page = await api.GetAsync<PageResponse<GigResponse>>(
                            "api/gigs" + query
                        );
                        items = page!.Items.Select(x =>
                            ListingItem.From(
                                x,
                                saved.Any(s => s.TargetType == "gig" && s.TargetId == x.Id)
                            )
                        );
                        CanLoadMore = SignedIn && pageNumber * page.PageSize < page.Total;
                        ResultLabel = $"{page.Total} open opportunities";
                    }
                }
                else if (Tab == "Gigs")
                    items = IsHiring
                        ? (await api.GetAsync<GigResponse[]>("api/me/gigs") ?? []).Select(x =>
                            ListingItem.From(x)
                        )
                        : (
                            await api.GetAsync<ApplicationResponse[]>("api/me/applications") ?? []
                        ).Select(ListingItem.From);
                else if (Tab == "Inbox")
                    items = (await api.GetAsync<ThreadResponse[]>("api/threads") ?? []).Select(
                        ListingItem.From
                    );
                else if (Tab == "Updates")
                    items = (await api.GetAsync<UpdateResponse[]>("api/me/updates") ?? []).Select(
                        ListingItem.From
                    );
                else
                {
                    var collection = new List<ListingItem>();
                    foreach (
                        var bookmark in saved.Where(x =>
                            x.TargetType == (IsHiring ? "profile" : "gig")
                        )
                    )
                        if (IsHiring)
                            collection.Add(
                                ListingItem.From(
                                    (
                                        await api.GetAsync<ProfileResponse>(
                                            "api/profiles/" + bookmark.TargetId
                                        )
                                    )!,
                                    true
                                )
                            );
                        else
                            collection.Add(
                                ListingItem.From(
                                    (
                                        await api.GetAsync<GigResponse>(
                                            "api/gigs/" + bookmark.TargetId
                                        )
                                    )!,
                                    true
                                )
                            );
                    items = collection;
                }
                if (version != loadVersion)
                    return;
                Items = new ObservableCollection<ListingItem>(append ? Items.Concat(items) : items);
                Empty = Items.Count == 0;
                ShowGate = !SignedIn && Tab == "Home";
                OnPropertyChanged(nameof(Heading));
                OnPropertyChanged(nameof(ShowHero));
            });
        }
        finally
        {
            loadGate.Release();
        }
    }

    [RelayCommand]
    private async Task SwitchModeAsync(string mode)
    {
        Interlocked.Increment(ref loadVersion);
        await loadGate.WaitAsync();
        var changed = false;
        try
        {
            while (IsBusy)
                await Task.Delay(30);
            await RunAsync(async () =>
            {
                if (SignedIn)
                    await session.ChangeModeAsync(Enum.Parse<AccountMode>(mode));
                else
                {
                    GuestMode = Enum.Parse<AccountMode>(mode);
                    session.PreviewMode = GuestMode;
                }
                changed = true;
            });
        }
        finally
        {
            loadGate.Release();
        }
        OnPropertyChanged(nameof(IsHiring));
        OnPropertyChanged(nameof(ModeLabel));
        if (changed)
            await LoadAsync();
    }

    [RelayCommand]
    private async Task SelectCategoryAsync(CategoryItem item)
    {
        SelectedCategory = SelectedCategory == item.Title ? "" : item.Title;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NavigateAsync(string tab)
    {
        if (!SignedIn && tab != "Home")
        {
            await nav.SignInAsync();
            return;
        }
        if (tab == "Post")
        {
            await nav.PostAsync();
            return;
        }
        if (tab == "Profile")
        {
            await nav.AccountAsync();
            return;
        }
        Tab = tab;
        await LoadAsync();
    }

    [RelayCommand]
    private Task SignInAsync() => SignedIn ? nav.AccountAsync() : nav.SignInAsync();

    [RelayCommand]
    private Task OpenAsync(ListingItem item) =>
        RunAsync(async () =>
        {
            if (item.Value is UpdateResponse activity)
            {
                if (activity.GigId is not null)
                    await nav.DetailAsync(
                        ListingItem.From(
                            (await api.GetAsync<GigResponse>("api/gigs/" + activity.GigId))!
                        )
                    );
                return;
            }
            await nav.DetailAsync(item);
        });

    [RelayCommand]
    private Task SaveAsync(ListingItem item) =>
        RunAsync(async () =>
        {
            if (!SignedIn)
            {
                await nav.SignInAsync();
                return;
            }
            if (item.Kind is not ("profile" or "gig"))
                return;
            var saved = await api.GetAsync<BookmarkResponse[]>("api/me/bookmarks");
            var existing = saved?.FirstOrDefault(x =>
                x.TargetId.ToString() == item.Id && x.TargetType == item.Kind
            );
            if (existing is not null)
                await api.DeleteAsync("api/me/bookmarks/" + existing.Id);
            else
                await api.SendAsync<BookmarkResponse>(
                    "api/me/bookmarks",
                    new SaveBookmarkRequest(item.Kind, Guid.Parse(item.Id))
                );
            var index = Items.IndexOf(item);
            if (index >= 0)
                Items[index] = item with { Saved = existing is null };
        });
}
