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
}
