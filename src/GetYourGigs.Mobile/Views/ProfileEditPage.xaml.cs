using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class ProfileEditPage : ContentPage
{
    public ProfileEditViewModel ViewModel { get; }

    public ProfileEditPage(ProfileEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }
}
