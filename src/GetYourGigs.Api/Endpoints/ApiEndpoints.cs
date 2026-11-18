using GetYourGigs.Api.Hosting;
using GetYourGigs.Api.Services;
using GetYourGigs.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GetYourGigs.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapMarketplace(this WebApplication app)
    {
        if (HostedConfiguration.IsSupabase(app.Configuration))
            app.MapGet(
                    "/uploads/{name}",
                    Results<RedirectHttpResult, NotFound> (string name, IImageStorage storage) =>
                        storage.PublicUrl(name) is { } url
                            ? TypedResults.Redirect(url)
                            : TypedResults.NotFound()
                )
                .WithSummary("Read a previously uploaded public image from persistent storage");

        var api = app.MapGroup("/api").RequireRateLimiting("api");
        api.MapGet(
                "/config",
                (IConfiguration configuration, IEmailCodeSender sender) =>
                    TypedResults.Ok(
                        new PublicConfigResponse(
                            configuration["Google:ClientId"],
                            sender.UsesDevelopmentCode,
                            Categories.All
                        )
                    )
            )
            .WithSummary("Public sign-in configuration");
        api.MapAuth();
        api.MapAccount();
        api.MapCatalog();
        api.MapBooking();
        api.MapMessaging();
        api.MapUpload();
    }
}
