namespace GetYourGigs.Mobile.Services;

public interface IBrowserSignIn
{
    Task<AuthResponse> SignInAsync();
}
