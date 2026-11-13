using GetYourGigs.Mobile.ViewModels;
using GetYourGigs.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace GetYourGigs.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // The XAML InputBorder owns the field outline, including on iOS.
#if IOS || MACCATALYST
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
            "BentoInput",
            (handler, _) =>
            {
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            }
        );
#endif
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Manrope.ttf", "Manrope");
                fonts.AddFont("Fraunces.ttf", "Fraunces");
            });
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddSingleton(sp =>
        {
#if ANDROID
            const string local = "http://10.0.2.2:5080/";
#else
            const string local = "http://localhost:5080/";
#endif
            var address = Preferences.Default.Get("api-address", local);
            Preferences.Default.Set("api-address", address);
            return new GigsApiClient(
                new HttpClient
                {
                    BaseAddress = new Uri(address),
                    // Allow Render Free to wake without replaying a mutation.
                    Timeout = TimeSpan.FromSeconds(120),
                },
                sp.GetRequiredService<ITokenStore>()
            );
        });
        builder.Services.AddSingleton<AppSession>();
        builder.Services.AddSingleton<Navigator>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<AuthViewModel>();
        builder.Services.AddTransient<AuthPage>();
        builder.Services.AddTransient<ProfileEditViewModel>();
        builder.Services.AddTransient<ProfileEditPage>();
        builder.Services.AddTransient<GigEditViewModel>();
        builder.Services.AddTransient<GigEditPage>();
        builder.Services.AddTransient<DetailViewModel>();
        builder.Services.AddTransient<DetailPage>();
        builder.Services.AddTransient<ApplicantsViewModel>();
        builder.Services.AddTransient<ApplicantsPage>();
        builder.Services.AddTransient<ThreadViewModel>();
        builder.Services.AddTransient<ThreadPage>();
        builder.Services.AddTransient<ComposeViewModel>();
        builder.Services.AddTransient<ComposePage>();
        builder.Services.AddTransient<AccountViewModel>();
        builder.Services.AddTransient<AccountPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
