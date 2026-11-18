using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class AuthPage : ContentPage
{
    public AuthViewModel ViewModel { get; }

    public AuthPage(AuthViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }
}
