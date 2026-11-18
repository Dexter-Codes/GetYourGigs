namespace GetYourGigs.Mobile.Services;

public interface INavigator
{
    Task BackAsync();
    Task SignInAsync();
    Task DetailAsync(ListingItem item);
    Task ThreadAsync(ThreadResponse thread);
    Task ProfileAsync();
    Task PostAsync(GigResponse? existing = null);
    Task AccountAsync();
    Task ApplicantsAsync(GigResponse gig);
    Task ComposeAsync(
        string kind,
        Guid recipient,
        Guid? gig,
        string title,
        Guid? application = null
    );
}
