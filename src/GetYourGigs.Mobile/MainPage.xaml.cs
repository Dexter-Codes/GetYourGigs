using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
