using System.Net;
using GetYourGigs.Client;
using GetYourGigs.Contracts;

namespace GetYourGigs.Tests;

public sealed class ClientErrorTests
{
    [Theory]
    [InlineData("<html>Proxy failed</html>")]
    [InlineData("[]")]
    [InlineData("{\"detail\":42,\"errors\":[]}")]
    [InlineData("{\"errors\":{\"name\":\"invalid\"}}")]
    public async Task Malformed_unauthorized_response_clears_token_and_disposes_content(string body)
    {
        using var content = new TrackedContent(body);
        using var handler = new ErrorHandler(content);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };
        var tokens = new TokenStore();
        var client = new GigsApiClient(http, tokens);

        var error = await Assert.ThrowsAsync<ApiRequestException>(() =>
            client.GetAsync<AccountResponse>("api/me/")
        );

        Assert.Equal(HttpStatusCode.Unauthorized, error.Status);
        Assert.Equal("The server could not complete this request.", error.Message);
        Assert.Null(await tokens.GetAsync());
        Assert.True(content.Disposed);
    }

    private sealed class ErrorHandler(HttpContent content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct
        ) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = content }
            );
    }

    private sealed class TrackedContent(string value) : StringContent(value)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TokenStore : ITokenStore
    {
        private string? token = "expired-token";

        public Task<string?> GetAsync() => Task.FromResult(token);

        public Task SetAsync(string? value)
        {
            token = value;
            return Task.CompletedTask;
        }
    }
}
