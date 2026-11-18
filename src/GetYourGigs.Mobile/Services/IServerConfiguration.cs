namespace GetYourGigs.Mobile.Services;

public interface IServerConfiguration
{
    Task ChangeAsync(string address);
}
