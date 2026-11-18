using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GetYourGigs.Contracts;

namespace GetYourGigs.Client;

public interface ITokenStore
{
    Task<string?> GetAsync();
    Task SetAsync(string? token);
}

public sealed class ApiRequestException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>Shared, AOT-friendly HTTP client. The API remains the sole business-rule authority.</summary>
public sealed class GigsApiClient(HttpClient http, ITokenStore tokens)
{
    // HttpClient settings become immutable after its first request. Keep the
    // selected server separately and resolve each request before sending it.
    public Uri? BaseAddress { get; set; } = http.BaseAddress;

    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default) =>
        await RequestAsync<T>(HttpMethod.Get, path, null, ct);

    public async Task<T?> SendAsync<T>(
        string path,
        object? body = null,
        string method = "POST",
        CancellationToken ct = default
    ) => await RequestAsync<T>(new HttpMethod(method), path, body, ct);

    public async Task<UploadResponse> UploadAsync(
        Stream stream,
        string filename,
        CancellationToken ct = default
    )
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", Path.GetFileName(filename));
        using var response = await SendRequestAsync(
            HttpMethod.Post,
            "api/uploads",
            null,
            ct,
            content
        );
        var text = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize(
            text,
            (JsonTypeInfo<UploadResponse>)
                ContractsJsonContext.Default.GetTypeInfo(typeof(UploadResponse))!
        )!;
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        using var response = await SendRequestAsync(HttpMethod.Delete, path, null, ct);
    }

    private async Task<T?> RequestAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct
    )
    {
        using var response = await SendRequestAsync(method, path, body, ct);
        if (
            response.StatusCode == HttpStatusCode.NoContent
            || response.Content.Headers.ContentLength == 0
        )
            return default;
        var text = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrEmpty(text))
            return default;
        return JsonSerializer.Deserialize(
            text,
            (JsonTypeInfo<T>)ContractsJsonContext.Default.GetTypeInfo(typeof(T))!
        );
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct,
        HttpContent? content = null
    )
    {
        var address =
            BaseAddress
            ?? throw new InvalidOperationException("An API server address is required.");
        using var request = new HttpRequestMessage(method, new Uri(address, path));
        var token = await tokens.GetAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (content is not null)
            request.Content = content;
        else if (body is not null)
            request.Content = new StringContent(
                JsonSerializer.Serialize(
                    body,
                    ContractsJsonContext.Default.GetTypeInfo(body.GetType())!
                ),
                Encoding.UTF8,
                "application/json"
            );
        var response = await http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
            return response;
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                await tokens.SetAsync(null);
            var message = await ReadErrorAsync(response, ct);
            throw new ApiRequestException(response.StatusCode, message);
        }
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken ct
    )
    {
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var problem = json.RootElement;
            if (problem.ValueKind == JsonValueKind.Object)
            {
                if (
                    problem.TryGetProperty("detail", out var detail)
                    && detail.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(detail.GetString())
                )
                    return detail.GetString()!;

                if (
                    problem.TryGetProperty("errors", out var errors)
                    && errors.ValueKind == JsonValueKind.Object
                )
                {
                    var messages = errors
                        .EnumerateObject()
                        .Where(property => property.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(property => property.Value.EnumerateArray())
                        .Where(value => value.ValueKind == JsonValueKind.String)
                        .Select(value => value.GetString())
                        .Where(value => !string.IsNullOrWhiteSpace(value));
                    var message = string.Join(" ", messages);
                    if (message.Length > 0)
                        return message;
                }
            }
        }
        catch (JsonException)
        {
            // Proxies can return HTML or an empty body instead of Problem Details.
        }

        return response.StatusCode == HttpStatusCode.TooManyRequests
            ? "Please wait a moment and try again."
            : "The server could not complete this request.";
    }
}
