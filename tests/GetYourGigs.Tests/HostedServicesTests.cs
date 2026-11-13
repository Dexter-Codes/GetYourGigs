using System.Net;
using System.Text;
using System.Text.Json;
using GetYourGigs.Api.Hosting;
using GetYourGigs.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GetYourGigs.Tests;

public sealed class HostedServicesTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))
            )
            .Build();

    [Fact]
    public async Task Supabase_upload_preserves_validated_bytes_and_does_not_publish_the_service_key()
    {
        byte[]? received = null;
        using var handler = new StubHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.StartsWith(
                "https://project.supabase.co/storage/v1/object/gyg-photos/",
                request.RequestUri!.AbsoluteUri
            );
            Assert.Equal("test-service-key", request.Headers.Authorization!.Parameter);
            Assert.Equal("false", request.Headers.GetValues("x-upsert").Single());
            Assert.Equal("image/png", request.Content!.Headers.ContentType!.MediaType);
            received = await request.Content.ReadAsByteArrayAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://project.supabase.co/"),
        };
        var storage = new SupabaseImageStorage(
            http,
            Config(
                ("Storage:Supabase:Bucket", "gyg-photos"),
                ("Storage:Supabase:ServiceRoleKey", "test-service-key")
            )
        );
        var bytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII="
        );
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "misleading.exe");
        var result = await new ImageService(storage).UploadAsync(file, default);
        Assert.Equal(bytes, received);
        var name = result.Url.Split('/').Last();
        Assert.True(ImageService.IsStoredName(name));
        Assert.Equal(
            "https://project.supabase.co/storage/v1/object/public/gyg-photos/" + name,
            storage.PublicUrl(name)
        );
        Assert.DoesNotContain("test-service-key", storage.PublicUrl(name)!);
        Assert.Null(storage.PublicUrl("../secret"));
        Assert.Null(storage.PublicUrl(name + "\n"));
    }

    [Fact]
    public async Task Failed_storage_returns_a_retryable_error_instead_of_a_broken_photo_url()
    {
        using var handler = new StubHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
        );
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://project.supabase.co/"),
        };
        var storage = new SupabaseImageStorage(
            http,
            Config(
                ("Storage:Supabase:Bucket", "photos"),
                ("Storage:Supabase:ServiceRoleKey", "key")
            )
        );
        var error = await Assert.ThrowsAsync<ApiException>(() =>
            storage.SaveAsync(
                Guid.NewGuid().ToString("N") + ".png",
                new MemoryStream([1, 2, 3]),
                "image/png",
                default
            )
        );
        Assert.Equal(503, error.Status);
    }

    [Fact]
    public async Task Production_never_exposes_development_codes_when_email_is_unconfigured()
    {
        var sender = new EmailCodeSender(
            Config(),
            new HostEnvironment { EnvironmentName = "Production" },
            new StubClientFactory(new HttpClient())
        );
        Assert.False(sender.UsesDevelopmentCode);
        var error = await Assert.ThrowsAsync<ApiException>(() =>
            sender.SendAsync("person@example.test", "123456", default)
        );
        Assert.Equal(503, error.Status);
    }

    [Fact]
    public async Task Resend_sends_codes_over_https_and_never_enables_the_development_fallback()
    {
        using var handler = new StubHandler(async request =>
        {
            Assert.Equal("https://api.resend.com/emails", request.RequestUri!.AbsoluteUri);
            Assert.Equal("test-resend-key", request.Headers.Authorization!.Parameter);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal("person@example.test", json.RootElement.GetProperty("to")[0].GetString());
            Assert.Contains("123456", json.RootElement.GetProperty("text").GetString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"test\"}"),
            };
        });
        using var http = new HttpClient(handler);
        var sender = new EmailCodeSender(
            Config(
                ("Email:Resend:ApiKey", "test-resend-key"),
                ("Email:From", "Get Your Gigs <signin@example.test>")
            ),
            new HostEnvironment { EnvironmentName = "Development" },
            new StubClientFactory(http)
        );
        Assert.False(sender.UsesDevelopmentCode);
        await sender.SendAsync("person@example.test", "123456", default);
    }

    [Theory]
    [InlineData("Storage:Provider", "unknown")]
    [InlineData("Hosting:RenderProxy", "true")]
    [InlineData("RENDER", "true")]
    [InlineData("Email:Resend:ApiKey", "key-without-sender")]
    [InlineData("Storage:Provider", "Supabase")]
    public void Incomplete_or_unsafe_host_configuration_fails_before_serving_requests(
        string key,
        string value
    ) =>
        Assert.Throws<InvalidOperationException>(() =>
            HostedConfiguration.Validate(Config((key, value)))
        );

    [Fact]
    public void Render_accepts_google_only_sign_in_with_persistent_storage()
    {
        HostedConfiguration.Validate(
            Config(
                ("RENDER", "true"),
                ("Hosting:RenderProxy", "true"),
                ("Storage:Provider", "Supabase"),
                ("Storage:Supabase:Url", "https://project.supabase.co"),
                ("Storage:Supabase:Bucket", "gyg-photos"),
                ("Storage:Supabase:ServiceRoleKey", "test-key"),
                ("Google:ClientId", "test-client")
            )
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Forwarding_is_opt_in_for_render_and_only_consumes_one_proxy_hop(bool render)
    {
        var options = new ForwardedHeadersOptions();
        HostedConfiguration.ConfigureForwarding(
            Config(("Hosting:RenderProxy", render.ToString())),
            options
        );
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.20");
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("getyourgigs.onrender.com");
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "192.0.2.9, 198.51.100.10";
        context.Request.Headers["X-Forwarded-Host"] = "attacker.example";
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(options)
        );
        await middleware.Invoke(context);
        Assert.Equal(render ? "https" : "http", context.Request.Scheme);
        Assert.Equal(
            render ? "198.51.100.10" : "203.0.113.20",
            context.Connection.RemoteIpAddress!.ToString()
        );
        Assert.Equal("getyourgigs.onrender.com", context.Request.Host.Value);
    }

    internal sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct
        ) => send(request);
    }

    internal sealed class StubClientFactory(HttpClient http) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => http;
    }

    private sealed class HostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "GetYourGigs.Tests";
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = "/tmp";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "/tmp";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
