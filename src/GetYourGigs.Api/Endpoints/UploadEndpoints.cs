using GetYourGigs.Api.Services;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Endpoints;

public static class UploadEndpoints
{
    public static void MapUpload(this RouteGroupBuilder api)
    {
        api.MapPost(
                "/uploads",
                async (HttpRequest request, IImageService images, CancellationToken ct) =>
                {
                    Require(request.HasFormContentType, "Choose an image file.");
                    var form = await request.ReadFormAsync(ct);
                    var uploaded = await images.UploadAsync(form.Files.GetFile("file"), ct);
                    return TypedResults.Created(uploaded.Url, uploaded);
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithSummary("Upload a profile or gig image up to 5 MB");
    }
}
