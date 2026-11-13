using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class ApplicantsPage : ContentPage
{
    public ApplicantsViewModel ViewModel { get; }

    public ApplicantsPage(ApplicantsViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }

    private async void OnShortlist(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: ApplicationResponse app })
            await ViewModel.ChangeAsync(app, ApplicationStatus.Shortlisted);
    }

    private async void OnBook(object? sender, EventArgs e)
    {
        if (
            sender is Button { BindingContext: ApplicationResponse app }
            && await DisplayAlertAsync(
                "Book this talent?",
                "Other applicants will be waitlisted. This talent’s conflicting applications on the same date will be cancelled.",
                "Book talent",
                "Go back"
            )
        )
            await ViewModel.ChangeAsync(app, ApplicationStatus.Booked);
    }

    private async void OnDecline(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: ApplicationResponse app })
            await ViewModel.ChangeAsync(app, ApplicationStatus.Declined);
    }
}
