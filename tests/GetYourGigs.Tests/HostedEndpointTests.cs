using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GetYourGigs.Tests;

public sealed class HostedEndpointTests(TestEnvironment environment)
    : IClassFixture<TestEnvironment>
{
    private WebApplicationFactory<Program> Hosted(HttpClient outbound, bool storage = false) =>
        environment.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Email:Resend:ApiKey", "test-resend-key");
            builder.UseSetting("Email:From", "Get Your Gigs <signin@example.test>");
            if (storage)
            {
                builder.UseSetting("Storage:Provider", "Supabase");
                builder.UseSetting("Storage:Supabase:Url", "https://project.supabase.co");
                builder.UseSetting("Storage:Supabase:ServiceRoleKey", "test-service-key");
                builder.UseSetting("Storage:Supabase:Bucket", "gyg-photos");
            }
            builder.ConfigureServices(services =>
                services.AddSingleton<IHttpClientFactory>(
                    new HostedServicesTests.StubClientFactory(outbound)
                )
            );
        });

    [Fact]
    public async Task Production_email_sign_in_uses_delivery_and_never_returns_the_code()
    {
        string? sentCode = null;
        using var handler = new HostedServicesTests.StubHandler(async request =>
        {
            Assert.Equal("https://api.resend.com/emails", request.RequestUri!.AbsoluteUri);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            sentCode = Regex
                .Match(json.RootElement.GetProperty("text").GetString()!, "\\b[0-9]{6}\\b")
                .Value;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"test\"}"),
            };
        });
        using var outbound = new HttpClient(handler);
        await using var factory = Hosted(outbound);
        using var client = factory.CreateClient(
            new() { BaseAddress = new Uri("https://localhost") }
        );
        var email = Guid.NewGuid() + "@example.test";
        using var response = await client.PostAsJsonAsync(
            "/api/auth/email/code",
            new EmailCodeRequest(email)
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EmailCodeResponse>();
        Assert.Null(result!.DevelopmentCode);
        Assert.False(string.IsNullOrWhiteSpace(sentCode));
        using var verify = await client.PostAsJsonAsync(
            "/api/auth/email/verify",
            new VerifyEmailRequest(email, sentCode!, "Hosted tester")
        );
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        using var config = await client.GetAsync("/api/config");
        var publicConfig = await config.Content.ReadFromJsonAsync<PublicConfigResponse>();
        Assert.DoesNotContain("test-resend-key", await config.Content.ReadAsStringAsync());
        Assert.False(publicConfig!.DevelopmentEmail);
    }

    [Fact]
    public async Task Failed_email_delivery_rolls_back_the_challenge_and_allows_a_retry()
    {
        using var handler = new HostedServicesTests.StubHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
        );
        using var outbound = new HttpClient(handler);
        await using var factory = Hosted(outbound);
        using var client = factory.CreateClient(
            new() { BaseAddress = new Uri("https://localhost") }
        );
        var email = Guid.NewGuid() + "@example.test";
        using var response = await client.PostAsJsonAsync(
            "/api/auth/email/code",
            new EmailCodeRequest(email)
        );
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.False(
            await scope
                .ServiceProvider.GetRequiredService<MarketplaceDb>()
                .EmailChallenges.AnyAsync(c => c.Email == email)
        );
    }

    [Fact]
    public async Task Hosted_upload_returns_a_stable_url_and_redirects_to_public_storage_without_credentials()
    {
        using var handler = new HostedServicesTests.StubHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))
        );
        using var outbound = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://project.supabase.co/"),
        };
        await using var factory = Hosted(outbound, storage: true);
        using var client = factory.CreateClient(
            new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }
        );
        var jwt = new JwtSecurityToken(
            "get-your-gigs",
            "get-your-gigs-clients",
            [new Claim("sub", Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(MarketplaceFactory.Key)),
                SecurityAlgorithms.HmacSha256
            )
        );
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(jwt)
        );
        using var form = new MultipartFormDataContent();
        form.Add(
            new ByteArrayContent(
                Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII="
                )
            ),
            "file",
            "photo.png"
        );
        using var upload = await client.PostAsync("/api/uploads", form);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var image = await upload.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.StartsWith("/uploads/", image!.Url);
        Assert.Equal(image.Url, upload.Headers.Location!.OriginalString);
        client.DefaultRequestHeaders.Authorization = null;
        using var read = await client.GetAsync(image.Url);
        Assert.Equal(HttpStatusCode.Redirect, read.StatusCode);
        Assert.StartsWith(
            "https://project.supabase.co/storage/v1/object/public/gyg-photos/",
            read.Headers.Location!.AbsoluteUri
        );
        Assert.DoesNotContain("test-service-key", read.Headers.Location.AbsoluteUri);
        using var invalid = await client.GetAsync("/uploads/not-a-generated-image.png");
        Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);
    }
}
