using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using GetYourGigs.Api.Data;
using GetYourGigs.Api.Endpoints;
using GetYourGigs.Api.Hosting;
using GetYourGigs.Api.Middleware;
using GetYourGigs.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Configuration.GetValue<bool>("SkipLocalConfig"))
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.AddHostedServices();
var signingKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
        throw new InvalidOperationException("Set Jwt__Key to a secret of at least 32 characters.");
    signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    builder.Configuration["Jwt:Key"] = signingKey;
}
if (signingKey.Length < 32)
    throw new InvalidOperationException("Jwt__Key must contain at least 32 characters.");
if (
    !builder.Environment.IsDevelopment()
    && !builder.Environment.IsEnvironment("Testing")
    && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Database"))
)
    throw new InvalidOperationException(
        "Set ConnectionStrings__Database for your PostgreSQL server."
    );
builder.Services.AddDbContext<MarketplaceDb>(o =>
    o.UseNpgsql(
        builder.Configuration.GetConnectionString("Database")
            ?? "Host=localhost;Port=55432;Database=getyourgigs;Username=gyg;Password=local-development"
    )
);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "get-your-gigs",
            ValidateAudience = true,
            ValidAudience = "get-your-gigs-clients",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 6 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 6 * 1024 * 1024);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy(
        "api",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst("sub")?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 240,
                    Window = TimeSpan.FromMinutes(1),
                }
            )
    );
    o.AddPolicy(
        "auth",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                }
            )
    );
});
var app = builder.Build();
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(
    async (context, next) =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.Headers.CacheControl = "no-store";
        await next();
    }
);
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseDefaultFiles();
app.UseStaticFiles();
if (!HostedConfiguration.IsSupabase(app.Configuration))
{
    var uploadPath = Path.Combine(app.Environment.ContentRootPath, ".data", "uploads");
    Directory.CreateDirectory(uploadPath);
    app.UseStaticFiles(
        new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadPath),
            RequestPath = "/uploads",
        }
    );
}
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" }));
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapMarketplace();
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = 404;
        return;
    }
    var path = Path.Combine(
        app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"),
        "index.html"
    );
    if (File.Exists(path))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(path);
    }
    else
        context.Response.StatusCode = 404;
});
if (
    args.Contains("--migrate")
    || (
        app.Environment.IsDevelopment()
        && builder.Configuration.GetValue("Database:AutoMigrate", true)
    )
)
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<MarketplaceDb>().Database.MigrateAsync();
}
if (args.Contains("--seed"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await DemoSeed.RunAsync(scope.ServiceProvider.GetRequiredService<MarketplaceDb>());
}
if (!args.Contains("--migrate-only"))
    app.Run();

public partial class Program { }
