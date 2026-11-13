using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using GetYourGigs.Contracts;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Services;

public interface IImageStorage
{
    Task SaveAsync(string name, Stream content, string contentType, CancellationToken ct);
    string? PublicUrl(string name);
}

public sealed class LocalImageStorage(IWebHostEnvironment environment) : IImageStorage
{
    public async Task SaveAsync(
        string name,
        Stream content,
        string contentType,
        CancellationToken ct
    )
    {
        var folder = Path.Combine(environment.ContentRootPath, ".data", "uploads");
        Directory.CreateDirectory(folder);
        await using var output = File.Create(Path.Combine(folder, name));
        await content.CopyToAsync(output, ct);
    }

    public string? PublicUrl(string name) => null;
}

public sealed class SupabaseImageStorage(HttpClient http, IConfiguration config) : IImageStorage
{
    private string ObjectPath(string name) =>
        Uri.EscapeDataString(config["Storage:Supabase:Bucket"]!) + "/" + Uri.EscapeDataString(name);

    public async Task SaveAsync(
        string name,
        Stream content,
        string contentType,
        CancellationToken ct
    )
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "storage/v1/object/" + ObjectPath(name)
        );
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            config["Storage:Supabase:ServiceRoleKey"]
        );
        request.Headers.Add("apikey", config["Storage:Supabase:ServiceRoleKey"]);
        request.Headers.Add("x-upsert", "false");
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        try
        {
            using var response = await http.SendAsync(request, ct);
            Require(
                response.IsSuccessStatusCode,
                "Photo storage is unavailable. Please try again.",
                503
            );
        }
        catch (Exception e)
            when (e is HttpRequestException
                || e is OperationCanceledException && !ct.IsCancellationRequested
            )
        {
            throw new ApiException(503, "Photo storage is unavailable. Please try again.");
        }
    }

    public string? PublicUrl(string name) =>
        ImageService.IsStoredName(name)
            ? new Uri(http.BaseAddress!, "storage/v1/object/public/" + ObjectPath(name)).AbsoluteUri
            : null;
}

public interface IImageService
{
    Task<UploadResponse> UploadAsync(IFormFile? file, CancellationToken ct);
}

public sealed partial class ImageService(IImageStorage storage) : IImageService
{
    public const int MaximumBytes = 5 * 1024 * 1024;

    [GeneratedRegex("\\A[a-f0-9]{32}\\.(png|jpg|webp)\\z", RegexOptions.CultureInvariant)]
    private static partial Regex StoredName();

    public static bool IsStoredName(string name) => StoredName().IsMatch(name);

    public async Task<UploadResponse> UploadAsync(IFormFile? file, CancellationToken ct)
    {
        Require(
            file is not null && file.Length > 0 && file.Length <= MaximumBytes,
            "Choose an image up to 5 MB."
        );
        // Buffer at most the accepted image size; do not trust the filename or MIME header.
        await using var input = file!.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(chunk, ct)) > 0)
        {
            Require(buffer.Length + count <= MaximumBytes, "Choose an image up to 5 MB.");
            await buffer.WriteAsync(chunk.AsMemory(0, count), ct);
        }
        var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        var extension =
            bytes.Length >= 8
            && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                ? ".png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
            : bytes.Length >= 12
            && bytes[..4].SequenceEqual("RIFF"u8)
            && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)
                ? ".webp"
            : null;
        Require(extension is not null, "Use a JPEG, PNG, or WebP image.");
        var name = Guid.NewGuid().ToString("N") + extension;
        var contentType = extension == ".jpg" ? "image/jpeg" : "image/" + extension![1..];
        buffer.Position = 0;
        await storage.SaveAsync(name, buffer, contentType, ct);
        return new UploadResponse("/uploads/" + name);
    }
}
