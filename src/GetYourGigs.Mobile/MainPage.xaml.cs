using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel vm;
    private bool initialized;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = this.vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!initialized)
        {
            initialized = true;
            await vm.InitializeAsync();
        }
        else
            await vm.LoadAsync();
    }
}
