using System.Collections.ObjectModel;

namespace GetYourGigs.Mobile.ViewModels;

public partial class ApplicantsViewModel(GigsApiClient api, INavigator nav, IUserDialogs dialogs)
    : ViewModelBase
{
    public GigResponse? Gig { get; set; }

    [ObservableProperty]
    private ObservableCollection<ApplicationResponse> items = [];
    public string Title => Gig?.Title ?? "Meet your applicants";

    [RelayCommand]
    public Task LoadAsync() =>
        RunAsync(async () =>
        {
            Items = new(
                await api.GetAsync<ApplicationResponse[]>($"api/gigs/{Gig!.Id}/applications") ?? []
            );
            OnPropertyChanged(nameof(Title));
        });

    [RelayCommand]
    private Task ShortlistAsync(ApplicationResponse application) =>
        ChangeAsync(application, ApplicationStatus.Shortlisted);

    [RelayCommand]
    private Task DeclineAsync(ApplicationResponse application) =>
        ChangeAsync(application, ApplicationStatus.Declined);

    [RelayCommand]
    private Task BookAsync(ApplicationResponse application) =>
        ChangeAsync(application, ApplicationStatus.Booked);

    private Task ChangeAsync(ApplicationResponse app, ApplicationStatus status) =>
        RunAsync(async () =>
        {
            if (
                status == ApplicationStatus.Booked
                && !await dialogs.ConfirmAsync(
                    "Book this talent?",
                    "Other applicants will be waitlisted. This talent’s conflicting applications on the same date will be cancelled.",
                    "Book talent",
                    "Go back"
                )
            )
                return;
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
