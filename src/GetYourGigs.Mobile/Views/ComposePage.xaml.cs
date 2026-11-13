using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class ComposePage : ContentPage
{
    public ComposeViewModel ViewModel { get; }

    public ComposePage(ComposeViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }
}
