using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class ThreadPage : ContentPage
{
    public ThreadViewModel ViewModel { get; }
    private IDispatcherTimer? timer;

    public ThreadPage(ThreadViewModel vm)
    {
        InitializeComponent();
        BindingContext = ViewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(10);
        timer.Tick += OnTick;
        timer.Start();
    }

    private async void OnTick(object? sender, EventArgs e) => await ViewModel.LoadAsync();

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (timer is not null)
        {
            timer.Stop();
            timer.Tick -= OnTick;
            timer = null;
        }
    }
}
