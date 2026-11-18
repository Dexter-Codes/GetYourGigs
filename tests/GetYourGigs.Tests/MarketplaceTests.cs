using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GetYourGigs.Api.Data;
using GetYourGigs.Api.Services;
using GetYourGigs.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace GetYourGigs.Tests;

public sealed class MarketplaceFactory : WebApplicationFactory<Program>
{
    public const string Key = "tests-only-signing-key-with-at-least-32-characters";
    private readonly string database = "gyg_tests_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("SkipLocalConfig", "true");
        var connection =
            Environment.GetEnvironmentVariable("GYG_TEST_DATABASE")
            ?? "Host=localhost;Port=55432;Database=postgres;Username=gyg";
        var cs = new Npgsql.NpgsqlConnectionStringBuilder(connection) { Database = database };
        builder.UseSetting("ConnectionStrings:Database", cs.ConnectionString);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MarketplaceDb>>();
            services.AddDbContext<MarketplaceDb>(options => options.UseNpgsql(cs.ConnectionString));
        });
        builder.UseSetting("Jwt:Key", Key);
    }
}

public sealed class TestEnvironment : IAsyncLifetime
{
    public MarketplaceFactory Factory { get; } = new();

    public Task InitializeAsync()
    {
        using var client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MarketplaceDb>();
            if (
                !db
                    .Database.GetDbConnection()
                    .Database.StartsWith("gyg_tests_", StringComparison.Ordinal)
            )
                throw new InvalidOperationException(
                    "Refusing to delete a database outside the isolated test namespace."
                );
            await db.Database.EnsureDeletedAsync();
        }
        await Factory.DisposeAsync();
    }
}

public sealed class MarketplaceTests(TestEnvironment environment) : IClassFixture<TestEnvironment>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private HttpClient Anonymous() => environment.Factory.CreateClient();

    private async Task<(HttpClient Client, Guid Id, Guid ProfileId)> PersonAsync(
        bool profile = true
    )
    {
        await using var scope = environment.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDb>();
        var account = new Account
        {
            Email = Guid.NewGuid() + "@example.test",
            Name = "Test person",
        };
        db.Accounts.Add(account);
        var p = new TalentProfile
        {
            OwnerId = account.Id,
            Headline = "Independent musician",
            Category = Categories.All[0],
            City = "Mumbai",
            Bio = "Experienced musician for intimate events and great collaborations.",
            Rate = 5000,
        };
        if (profile)
            db.Profiles.Add(p);
        await db.SaveChangesAsync();
        var jwt = new JwtSecurityToken(
            "get-your-gigs",
            "get-your-gigs-clients",
            [new Claim("sub", account.Id.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(MarketplaceFactory.Key)),
                SecurityAlgorithms.HmacSha256
            )
        );
        var client = Anonymous();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(jwt)
        );
        return (client, account.Id, p.Id);
    }

    private static Task<HttpResponseMessage> Post(HttpClient c, string path, object? body = null) =>
        c.PostAsJsonAsync("/api" + path, body, Json);

    private static Task<HttpResponseMessage> Put(HttpClient c, string path, object body) =>
        c.PutAsJsonAsync("/api" + path, body, Json);

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {text}");
        return JsonSerializer.Deserialize<T>(text, Json)!;
    }

    private async Task<GigResponse> Gig(HttpClient hirer, DateOnly? date = null) =>
        await Read<GigResponse>(
            await Post(
                hirer,
                "/gigs/",
                new SaveGigRequest(
                    "An intimate acoustic performance",
                    Categories.All[0],
                    "Mumbai",
                    "We need an experienced acoustic musician for a small family celebration.",
                    date
                        ?? DateOnly.FromDateTime(
                            TimeZoneInfo
                                .ConvertTimeBySystemTimeZoneId(
                                    DateTimeOffset.UtcNow,
                                    "Asia/Kolkata"
                                )
                                .DateTime
                        ),
                    10000
                )
            )
        );

    private static async Task<ApplicationResponse> Apply(HttpClient c, Guid gig) =>
        await Read<ApplicationResponse>(
            await Post(
                c,
                $"/gigs/{gig}/applications",
                new ApplyRequest(
                    "I am a very good fit with relevant experience and availability.",
                    5000
                )
            )
        );

    [Fact]
    public async Task Anonymous_preview_is_capped_even_with_arbitrary_page_and_me_is_private()
    {
        for (var i = 0; i < 4; i++)
            await PersonAsync();
        using var c = Anonymous();
        var page = await Read<PageResponse<ProfileResponse>>(
            await c.GetAsync("/api/profiles?page=999")
        );
        Assert.Equal(3, page.Items.Length);
        Assert.True(page.IsPreview);
        Assert.Equal(1, page.Page);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/me/")).StatusCode);
    }

    [Fact]
    public async Task Same_account_can_switch_context_and_preference_survives_new_request()
    {
        var (c, id, _) = await PersonAsync();
        using (c)
        {
            var mode = await Read<AccountResponse>(
                await Put(c, "/me/mode", new UpdateModeRequest(AccountMode.Working))
            );
            Assert.Equal(id, mode.Id);
            Assert.Equal(
                AccountMode.Working,
                (await Read<AccountResponse>(await c.GetAsync("/api/me/"))).PreferredMode
            );
            await Put(c, "/me/mode", new UpdateModeRequest(AccountMode.Hiring));
            Assert.Equal(
                AccountMode.Hiring,
                (await Read<AccountResponse>(await c.GetAsync("/api/me/"))).PreferredMode
            );
        }
    }

    [Fact]
    public async Task Four_parallel_applications_allow_exactly_three()
    {
        var (h, _, _) = await PersonAsync(false);
        var (w, _, _) = await PersonAsync();
        using (h)
        using (w)
        {
            var gigs = new List<GigResponse>();
            for (var i = 0; i < 4; i++)
                gigs.Add(await Gig(h));
            var responses = await Task.WhenAll(
                gigs.Select(g =>
                    Post(
                        w,
                        $"/gigs/{g.Id}/applications",
                        new ApplyRequest(
                            "I bring substantial experience to this performance.",
                            null
                        )
                    )
                )
            );
            Assert.Equal(3, responses.Count(x => x.StatusCode == HttpStatusCode.Created));
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.TooManyRequests);
            var allowance = await Read<AllowanceResponse>(await w.GetAsync("/api/me/allowance"));
            Assert.Equal(3, allowance.Used);
        }
    }

    [Fact]
    public async Task Booking_waitlists_others_and_cancels_same_day_conflicts()
    {
        var (h, _, _) = await PersonAsync(false);
        var (w, _, _) = await PersonAsync();
        var (other, _, _) = await PersonAsync();
        using (h)
        using (w)
        using (other)
        {
            var g1 = await Gig(h);
            var g2 = await Gig(h);
            var winner = await Apply(w, g1.Id);
            var conflict = await Apply(w, g2.Id);
            var sibling = await Apply(other, g1.Id);
            var booked = await Read<ApplicationResponse>(
                await Put(
                    h,
                    $"/applications/{winner.Id}/status",
                    new ApplicationActionRequest(ApplicationStatus.Booked)
                )
            );
            Assert.Equal(ApplicationStatus.Booked, booked.Status);
            var apps = await Read<ApplicationResponse[]>(
                await h.GetAsync($"/api/gigs/{g1.Id}/applications")
            );
            Assert.Equal(ApplicationStatus.Waitlisted, apps.Single(x => x.Id == sibling.Id).Status);
            var own = await Read<ApplicationResponse[]>(await w.GetAsync("/api/me/applications"));
            Assert.Equal(ApplicationStatus.Cancelled, own.Single(x => x.Id == conflict.Id).Status);
            Assert.Equal(
                HttpStatusCode.Conflict,
                (
                    await Post(
                        w,
                        $"/gigs/{g2.Id}/applications",
                        new ApplyRequest("A second application should never get through.", null)
                    )
                ).StatusCode
            );
        }
    }

    [Fact]
    public async Task Concurrent_bookings_on_same_gig_have_only_one_winner()
    {
        var (h, _, _) = await PersonAsync(false);
        var (a, _, _) = await PersonAsync();
        var (b, _, _) = await PersonAsync();
        using (h)
        using (a)
        using (b)
        {
            var g = await Gig(h);
            var aa = await Apply(a, g.Id);
            var ab = await Apply(b, g.Id);
            var results = await Task.WhenAll(
                new[] { aa, ab }.Select(x =>
                    Put(
                        h,
                        $"/applications/{x.Id}/status",
                        new ApplicationActionRequest(ApplicationStatus.Booked)
                    )
                )
            );
            Assert.Single(results, x => x.IsSuccessStatusCode);
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task Competing_hirers_cannot_double_book_same_person_on_same_day()
    {
        var (h1, _, _) = await PersonAsync(false);
        var (h2, _, _) = await PersonAsync(false);
        var (w, _, _) = await PersonAsync();
        using (h1)
        using (h2)
        using (w)
        {
            var g1 = await Gig(h1);
            var g2 = await Gig(h2);
            var a1 = await Apply(w, g1.Id);
            var a2 = await Apply(w, g2.Id);
            var results = await Task.WhenAll(
                Put(
                    h1,
                    $"/applications/{a1.Id}/status",
                    new ApplicationActionRequest(ApplicationStatus.Booked)
                ),
                Put(
                    h2,
                    $"/applications/{a2.Id}/status",
                    new ApplicationActionRequest(ApplicationStatus.Booked)
                )
            );
            Assert.Single(results, x => x.IsSuccessStatusCode);
            var apps = await Read<ApplicationResponse[]>(await w.GetAsync("/api/me/applications"));
            Assert.Single(apps, x => x.Status == ApplicationStatus.Booked);
            Assert.Single(apps, x => x.Status == ApplicationStatus.Cancelled);
        }
    }

    [Fact]
    public async Task Review_requires_completion_and_can_only_be_written_once_by_hirer()
    {
        var (h, _, _) = await PersonAsync(false);
        var (w, _, profileId) = await PersonAsync();
        using (h)
        using (w)
        {
            var g = await Gig(h);
            var a = await Apply(w, g.Id);
            var request = new CreateReviewRequest(
                a.Id,
                5,
                "Excellent work and thoughtful communication throughout."
            );
            Assert.Equal(HttpStatusCode.Conflict, (await Post(h, "/reviews", request)).StatusCode);
            await Read<ApplicationResponse>(
                await Put(
                    h,
                    $"/applications/{a.Id}/status",
                    new ApplicationActionRequest(ApplicationStatus.Booked)
                )
            );
            Assert.Equal(HttpStatusCode.Conflict, (await Post(h, "/reviews", request)).StatusCode);
            await Read<GigResponse>(await Post(h, $"/gigs/{g.Id}/complete"));
            Assert.Equal(HttpStatusCode.Forbidden, (await Post(w, "/reviews", request)).StatusCode);
            await Read<ReviewResponse>(await Post(h, "/reviews", request));
            Assert.Equal(HttpStatusCode.Conflict, (await Post(h, "/reviews", request)).StatusCode);
            var profile = await Read<ProfileResponse>(
                await w.GetAsync("/api/profiles/" + profileId)
            );
            Assert.Equal(1, profile.ReviewCount);
            Assert.Equal(5, profile.Rating);
            // Detail, discovery, and owner reads share the projection contract.
            var mine = await Read<ProfileResponse>(await w.GetAsync("/api/me/profile"));
            Assert.Equal(profile.Rating, mine.Rating);
            Assert.Equal(profile.ReviewCount, mine.ReviewCount);
            await using var scope = environment.Factory.Services.CreateAsyncScope();
            var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
            var db = scope.ServiceProvider.GetRequiredService<MarketplaceDb>();
            var detail = await catalog.GigAsync(g.Id, CancellationToken.None);
            var owned = await catalog.MineAsync(g.OwnerId, CancellationToken.None);
            Assert.Equal(1, detail.ApplicationCount);
            Assert.Equal(1, Assert.Single(owned, item => item.Id == g.Id).ApplicationCount);
            Assert.Empty(db.ChangeTracker.Entries());
        }
    }

    [Fact]
    public async Task Future_gig_cannot_be_marked_complete()
    {
        var (h, _, _) = await PersonAsync(false);
        var (w, _, _) = await PersonAsync();
        using (h)
        using (w)
        {
            var g = await Gig(
                h,
                DateOnly
                    .FromDateTime(
                        TimeZoneInfo
                            .ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Kolkata")
                            .DateTime
                    )
                    .AddDays(4)
            );
            var a = await Apply(w, g.Id);
            await Read<ApplicationResponse>(
                await Put(
                    h,
                    $"/applications/{a.Id}/status",
                    new ApplicationActionRequest(ApplicationStatus.Booked)
                )
            );
            Assert.Equal(
                HttpStatusCode.Conflict,
                (await Post(h, $"/gigs/{g.Id}/complete")).StatusCode
            );
        }
    }

    [Fact]
    public async Task Private_messages_are_visible_only_to_participants_and_replies_persist()
    {
        var (a, _, _) = await PersonAsync();
        var (b, bid, _) = await PersonAsync();
        var (stranger, _, _) = await PersonAsync();
        using (a)
        using (b)
        using (stranger)
        {
            var thread = await Read<ThreadResponse>(
                await Post(
                    a,
                    "/threads/",
                    new StartThreadRequest(bid, null, "Hello, can we discuss a project?")
                )
            );
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await stranger.GetAsync($"/api/threads/{thread.Id}/messages")).StatusCode
            );
            Assert.Equal(
                HttpStatusCode.NotFound,
                (
                    await Post(
                        stranger,
                        $"/threads/{thread.Id}/messages",
                        new SendMessageRequest("Intruder")
                    )
                ).StatusCode
            );
            await Read<MessageResponse>(
                await Post(
                    b,
                    $"/threads/{thread.Id}/messages",
                    new SendMessageRequest("Of course. Tell me more about it.")
                )
            );
            Assert.Equal(
                2,
                (
                    await Read<MessageResponse[]>(
                        await a.GetAsync($"/api/threads/{thread.Id}/messages")
                    )
                ).Length
            );
        }
    }

    [Fact]
    public async Task Unauthorized_hirer_cannot_see_applicants_or_change_status()
    {
        var (h, _, _) = await PersonAsync(false);
        var (w, _, _) = await PersonAsync();
        var (other, _, _) = await PersonAsync(false);
        using (h)
        using (w)
        using (other)
        {
            var g = await Gig(h);
            var a = await Apply(w, g.Id);
            Assert.Equal(
                HttpStatusCode.Forbidden,
                (await other.GetAsync($"/api/gigs/{g.Id}/applications")).StatusCode
            );
            Assert.Equal(
                HttpStatusCode.Forbidden,
                (
                    await Put(
                        other,
                        $"/applications/{a.Id}/status",
                        new ApplicationActionRequest(ApplicationStatus.Booked)
                    )
                ).StatusCode
            );
        }
    }

    [Fact]
    public async Task Bookmarks_are_idempotent_and_private()
    {
        var (a, _, p) = await PersonAsync();
        var (b, _, _) = await PersonAsync();
        using (a)
        using (b)
        {
            var first = await Read<BookmarkResponse>(
                await Post(a, "/me/bookmarks", new SaveBookmarkRequest("profile", p))
            );
            var second = await Read<BookmarkResponse>(
                await Post(a, "/me/bookmarks", new SaveBookmarkRequest("profile", p))
            );
            Assert.Equal(first.Id, second.Id);
            Assert.Empty(await Read<BookmarkResponse[]>(await b.GetAsync("/api/me/bookmarks")));
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await b.DeleteAsync("/api/me/bookmarks/" + first.Id)).StatusCode
            );
        }
    }

    [Fact]
    public async Task Email_code_is_single_use_and_bad_attempts_are_limited()
    {
        using var c = Anonymous();
        var email = Guid.NewGuid() + "@example.test";
        var code = await Read<EmailCodeResponse>(
            await Post(c, "/auth/email/code", new EmailCodeRequest(email))
        );
        Assert.NotNull(code.DevelopmentCode);
        var request = new VerifyEmailRequest(email, code.DevelopmentCode!, "Verified person");
        var auth = await Read<AuthResponse>(await Post(c, "/auth/email/verify", request));
        Assert.Equal(email, auth.Account.Email);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Post(c, "/auth/email/verify", request)).StatusCode
        );
        var other = Guid.NewGuid() + "@example.test";
        var challenge = await Read<EmailCodeResponse>(
            await Post(c, "/auth/email/code", new EmailCodeRequest(other))
        );
        for (var i = 0; i < 5; i++)
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (
                    await Post(
                        c,
                        "/auth/email/verify",
                        new VerifyEmailRequest(other, "000000", "Verified person")
                    )
                ).StatusCode
            );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (
                await Post(
                    c,
                    "/auth/email/verify",
                    new VerifyEmailRequest(other, challenge.DevelopmentCode!, "Verified person")
                )
            ).StatusCode
        );
    }

    [Fact]
    public async Task Validation_rejects_short_briefs_and_invalid_ratings()
    {
        var (h, _, _) = await PersonAsync(false);
        using (h)
        {
            Assert.Equal(
                HttpStatusCode.BadRequest,
                (
                    await Post(
                        h,
                        "/gigs/",
                        new SaveGigRequest("x", Categories.All[0], "x", "short", null, -1)
                    )
                ).StatusCode
            );
            Assert.Equal(
                HttpStatusCode.BadRequest,
                (
                    await Post(h, "/reviews", new CreateReviewRequest(Guid.NewGuid(), 8, "bad"))
                ).StatusCode
            );
        }
    }

    [Fact]
    public async Task Mobile_handoff_requires_its_proof_key_and_is_single_use()
    {
        var (client, _, _) = await PersonAsync();
        using (client)
        {
            var verifier = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)
            );
            var challenge = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(
                System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes(verifier))
            );
            var grant = await Read<MobileGrantResponse>(
                await Post(client, "/auth/mobile/grant", new MobileGrantRequest(challenge))
            );
            using var anonymous = Anonymous();
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (
                    await Post(
                        anonymous,
                        "/auth/mobile/exchange",
                        new MobileExchangeRequest(grant.Code, new string('x', 43))
                    )
                ).StatusCode
            );
            var auth = await Read<AuthResponse>(
                await Post(
                    anonymous,
                    "/auth/mobile/exchange",
                    new MobileExchangeRequest(grant.Code, verifier)
                )
            );
            Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (
                    await Post(
                        anonymous,
                        "/auth/mobile/exchange",
                        new MobileExchangeRequest(grant.Code, verifier)
                    )
                ).StatusCode
            );
        }
    }

    [Fact]
    public async Task Upload_rejects_non_images_and_requires_an_account()
    {
        using var anonymous = Anonymous();
        using var upload = new MultipartFormDataContent();
        upload.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes("<script>not an image</script>")),
            "file",
            "fake.png"
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsync("/api/uploads", upload)).StatusCode
        );
        var (client, _, _) = await PersonAsync();
        using (client)
        {
            using var fake = new MultipartFormDataContent();
            fake.Add(
                new ByteArrayContent(Encoding.UTF8.GetBytes("<script>not an image</script>")),
                "file",
                "fake.png"
            );
            Assert.Equal(
                HttpStatusCode.BadRequest,
                (await client.PostAsync("/api/uploads", fake)).StatusCode
            );
        }
    }

    [Fact]
    public async Task Shared_client_deserializes_server_contracts_and_string_enums()
    {
        var (client, _, _) = await PersonAsync();
        using (client)
        {
            var shared = new GetYourGigs.Client.GigsApiClient(client, new TestTokenStore());
            var account = await shared.GetAsync<AccountResponse>("api/me/");
            Assert.NotNull(account);
            Assert.Equal(AccountMode.Hiring, account.PreferredMode);
            var updated = await shared.SendAsync<AccountResponse>(
                "api/me/mode",
                new UpdateModeRequest(AccountMode.Working),
                "PUT"
            );
            Assert.Equal(AccountMode.Working, updated!.PreferredMode);
            var profiles = await shared.GetAsync<PageResponse<ProfileResponse>>("api/profiles");
            Assert.NotNull(profiles);
            Assert.False(profiles.IsPreview);
        }
    }

    [Fact]
    public async Task Catalog_pages_with_equal_names_do_not_repeat_or_omit_profiles()
    {
        var city = "Test-" + Guid.NewGuid().ToString("N");
        var profileIds = new List<Guid>();
        for (var index = 0; index < 13; index++)
        {
            var (client, _, profileId) = await PersonAsync();
            client.Dispose();
            profileIds.Add(profileId);
        }
        await using var scope = environment.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDb>();
        await db
            .Profiles.Where(profile => profileIds.Contains(profile.Id))
            .ExecuteUpdateAsync(update => update.SetProperty(profile => profile.City, city));
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var first = await catalog.ProfilesAsync(
            true,
            null,
            null,
            city,
            null,
            1,
            CancellationToken.None
        );
        var second = await catalog.ProfilesAsync(
            true,
            null,
            null,
            city,
            null,
            2,
            CancellationToken.None
        );

        Assert.Equal(13, first.Total);
        Assert.Equal(12, first.Items.Length);
        Assert.Single(second.Items);
        Assert.Equal(
            profileIds.Order(),
            first.Items.Concat(second.Items).Select(profile => profile.Id).Order()
        );
        Assert.All(
            first.Items,
            profile =>
            {
                Assert.Null(profile.Rating);
                Assert.Equal(0, profile.ReviewCount);
            }
        );
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private sealed class TestTokenStore : GetYourGigs.Client.ITokenStore
    {
        public Task<string?> GetAsync() => Task.FromResult<string?>(null);

        public Task SetAsync(string? token) => Task.CompletedTask;
    }
}
