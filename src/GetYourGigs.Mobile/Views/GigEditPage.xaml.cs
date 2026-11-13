using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class GigEditPage : ContentPage
{
    public GigEditViewModel ViewModel { get; }

    public GigEditPage(GigEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }
}
