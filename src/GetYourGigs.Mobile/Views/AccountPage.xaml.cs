using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class AccountPage : ContentPage
{
    public AccountViewModel ViewModel { get; }

    public AccountPage(AccountViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }
}
