namespace GetYourGigs.Mobile.ViewModels;

public partial class ProfileEditViewModel(
    GigsApiClient api,
    AppSession session,
    INavigator nav,
    IPhotoPicker photoPicker
) : MediaEditorViewModel(api, photoPicker)
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
            var profile = await Api.GetAsync<ProfileResponse>("api/me/profile");
            if (profile is null)
                return;
            Headline = profile.Headline;
            Category = profile.Category;
            City = profile.City;
            Rate = profile.Rate.ToString(System.Globalization.CultureInfo.CurrentCulture);
            Bio = profile.Bio;
            Skills = string.Join(", ", profile.Skills);
            Available = profile.Available;
            Photos = new(profile.Photos);
            RateUnit = profile.RateUnit;
        });

    [RelayCommand]
    private Task SaveAsync() =>
        RunAsync(async () =>
        {
            if (!decimal.TryParse(Rate, out var rate))
                throw new ArgumentException("Enter a valid rate.");
            var request = new SaveProfileRequest(
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
            Validate(request);
            await Api.SendAsync<ProfileResponse>("api/me/profile", request, "PUT");
            await session.RefreshAsync();
            await nav.BackAsync();
        });
}
