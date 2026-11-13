using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class DetailPage : ContentPage
{
    public DetailViewModel ViewModel { get; }

    public DetailPage(DetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }

    private async void OnComplete(object? sender, EventArgs e)
    {
        if (
            await DisplayAlertAsync(
                "Did the gig take place?",
                "Confirm that the booked talent actually completed this gig. This enables a genuine review.",
                "Confirm completed",
                "Go back"
            )
        )
            await ViewModel.CompleteAsync();
    }

    private async void OnCloseGig(object? sender, EventArgs e)
    {
        if (
            await DisplayAlertAsync(
                "Close this gig?",
                "New applications will stop, and current applicants will be notified.",
                "Close gig",
                "Go back"
            )
        )
            await ViewModel.CloseAsync();
    }

    private async void OnWithdraw(object? sender, EventArgs e)
    {
        if (
            await DisplayAlertAsync(
                "Withdraw from this gig?",
                "The hirer will be notified. This does not restore today's application allowance.",
                "Withdraw",
                "Go back"
            )
        )
            await ViewModel.WithdrawAsync();
    }
}
