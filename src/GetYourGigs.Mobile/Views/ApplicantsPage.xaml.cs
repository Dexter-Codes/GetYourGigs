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
}
