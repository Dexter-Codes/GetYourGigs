using System.Security.Cryptography;
using System.Text;

namespace GetYourGigs.Mobile.Services;

/// <summary>Runs the platform browser handoff; only the API issues access tokens.</summary>
public sealed class BrowserSignIn(GigsApiClient api) : IBrowserSignIn
{
    public async Task<AuthResponse> SignInAsync()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var result = await WebAuthenticator.Default.AuthenticateAsync(
            new Uri(api.BaseAddress!, "?mobileChallenge=" + challenge),
            new Uri("getyourgigs://auth")
        );
        if (!result.Properties.TryGetValue("code", out var code))
            throw new ArgumentException("Mobile sign-in did not return a code.");
        return await api.SendAsync<AuthResponse>(
                "api/auth/mobile/exchange",
                new MobileExchangeRequest(code, verifier)
            )
            ?? throw new IOException(
                "Mobile sign-in returned an empty response. Please try again."
            );
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
