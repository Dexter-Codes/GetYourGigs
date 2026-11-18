using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class ThreadPage : ContentPage
{
    public ThreadViewModel ViewModel { get; }

    public ThreadPage(ThreadViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }
}
