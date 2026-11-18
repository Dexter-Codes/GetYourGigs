namespace GetYourGigs.Mobile.ViewModels;

public partial class ComposeViewModel(GigsApiClient api, INavigator nav) : ViewModelBase
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
                var request = new ApplyRequest(Text, price);
                Validate(request);
                await api.SendAsync<ApplicationResponse>($"api/gigs/{gig}/applications", request);
                await nav.BackAsync();
            }
            else if (kind == "review")
            {
                var request = new CreateReviewRequest(application!.Value, Rating, Text);
                Validate(request);
                await api.SendAsync<ReviewResponse>("api/reviews", request);
                await nav.BackAsync();
            }
            else
            {
                var request = new StartThreadRequest(recipient, gig, Text);
                Validate(request);
                var thread = await api.SendAsync<ThreadResponse>("api/threads", request);
                await nav.BackAsync();
                await nav.ThreadAsync(thread!);
            }
        });
}
