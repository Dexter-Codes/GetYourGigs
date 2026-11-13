using System.Net;
using GetYourGigs.Client;
using GetYourGigs.Contracts;

namespace GetYourGigs.Tests;

public sealed class ClientServerSwitchTests
{
    [Fact]
    public async Task Switching_server_after_a_request_routes_all_operations_to_the_new_server()
    {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://original.example/"),
        };
        var tokens = new MemoryTokenStore();
        var client = new GigsApiClient(http, tokens);

        await client.GetAsync<UploadResponse>("api/config");
        client.BaseAddress = new Uri("https://replacement.example/");
        await tokens.SetAsync(null);
        await client.GetAsync<UploadResponse>("api/config");
        await client.SendAsync<UploadResponse>(
            "api/auth/email/code",
            new EmailCodeRequest("test@example.test")
        );
        await client.UploadAsync(new MemoryStream([1, 2, 3]), "photo.png");
        await client.DeleteAsync("api/saved/123");

        Assert.Equal(
            [
                "https://original.example/api/config",
                "https://replacement.example/api/config",
                "https://replacement.example/api/auth/email/code",
                "https://replacement.example/api/uploads",
                "https://replacement.example/api/saved/123",
            ],
            handler.Requests.Select(r => r.Url)
        );
        Assert.Equal("old-server-token", handler.Requests[0].Token);
        Assert.All(handler.Requests.Skip(1), r => Assert.Null(r.Token));
        Assert.Equal(new Uri("https://original.example/"), http.BaseAddress);
    }

    private sealed class MemoryTokenStore : ITokenStore
    {
        private string? token = "old-server-token";

        public Task<string?> GetAsync() => Task.FromResult(token);

        public Task SetAsync(string? value)
        {
            token = value;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(string Url, string? Token)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct
        )
        {
            Requests.Add(
                (request.RequestUri!.ToString(), request.Headers.Authorization?.Parameter)
            );
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"url\":\"/uploads/photo.png\"}"),
                }
            );
        }
    }
}
