namespace GetYourGigs.Mobile.Services;

public interface IUserDialogs
{
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
