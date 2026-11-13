using GetYourGigs.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GetYourGigs.Mobile.Services;

public sealed class Navigator(IServiceProvider services)
{
    private static INavigation Navigation => Application.Current!.Windows[0].Page!.Navigation;

    public Task BackAsync() => Navigation.PopAsync();

    public Task SignInAsync() => Navigation.PushAsync(services.GetRequiredService<AuthPage>());

    public async Task DetailAsync(ListingItem item)
    {
        if (item.Value is ThreadResponse thread)
        {
            await ThreadAsync(thread);
            return;
        }
        var page = services.GetRequiredService<DetailPage>();
        page.ViewModel.Item = item;
        await Navigation.PushAsync(page);
    }

    public async Task ThreadAsync(ThreadResponse thread)
    {
        var page = services.GetRequiredService<ThreadPage>();
        page.ViewModel.Thread = thread;
        await Navigation.PushAsync(page);
    }

    public async Task ProfileAsync()
    {
        var page = services.GetRequiredService<ProfileEditPage>();
        await page.ViewModel.InitializeAsync();
        await Navigation.PushAsync(page);
    }

    public async Task PostAsync(GigResponse? existing = null)
    {
        var page = services.GetRequiredService<GigEditPage>();
        page.ViewModel.Initialize(existing);
        await Navigation.PushAsync(page);
    }

    public Task AccountAsync() => Navigation.PushAsync(services.GetRequiredService<AccountPage>());

    public async Task ApplicantsAsync(GigResponse gig)
    {
        var page = services.GetRequiredService<ApplicantsPage>();
        page.ViewModel.Gig = gig;
        await Navigation.PushAsync(page);
    }

    public async Task ComposeAsync(
        string kind,
        Guid recipient,
        Guid? gig,
        string title,
        Guid? application = null
    )
    {
        var page = services.GetRequiredService<ComposePage>();
        page.ViewModel.Initialize(kind, recipient, gig, title, application);
        await Navigation.PushAsync(page);
    }
}
