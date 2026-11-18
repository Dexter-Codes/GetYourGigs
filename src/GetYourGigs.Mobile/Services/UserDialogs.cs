namespace GetYourGigs.Mobile.Services;

public sealed class UserDialogs : IUserDialogs
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        MainThread.InvokeOnMainThreadAsync(() =>
        {
            var root =
                Application.Current?.Windows.FirstOrDefault()?.Page
                ?? throw new InvalidOperationException("No active window is available.");
            var page = root.Navigation.NavigationStack.LastOrDefault() ?? root;
            return page.DisplayAlertAsync(title, message, accept, cancel);
        });
}
