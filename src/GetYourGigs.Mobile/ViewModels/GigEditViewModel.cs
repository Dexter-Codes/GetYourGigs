namespace GetYourGigs.Mobile.ViewModels;

public partial class GigEditViewModel(GigsApiClient api, INavigator nav, IPhotoPicker photoPicker)
    : MediaEditorViewModel(api, photoPicker)
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

    public void Initialize(GigResponse? existingGig)
    {
        id = existingGig?.Id;
        if (existingGig is null)
            return;
        Title = existingGig.Title;
        Category = existingGig.Category;
        City = existingGig.City;
        Budget = existingGig.Budget.ToString(System.Globalization.CultureInfo.CurrentCulture);
        Description = existingGig.Description;
        HasDate = existingGig.Date is not null;
        Date = existingGig.Date?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
        Photos = new(existingGig.Photos);
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
            var request = new SaveGigRequest(
                Title,
                Category,
                City,
                Description,
                HasDate ? DateOnly.FromDateTime(Date) : null,
                budget,
                Photos.ToArray()
            );
            Validate(request);
            if (Step == 1)
            {
                Step++;
                return;
            }
            await Api.SendAsync<GigResponse>(
                "api/gigs/" + (id?.ToString() ?? ""),
                request,
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
