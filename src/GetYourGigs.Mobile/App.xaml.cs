using Microsoft.Extensions.DependencyInjection;

namespace GetYourGigs.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        this.services = services;
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(
            new NavigationPage(services.GetRequiredService<MainPage>())
            {
                BarBackgroundColor = (Color)Resources["Paper"],
                BarTextColor = (Color)Resources["Ink"],
            }
        );
}
