using GetYourGigs.Api.Data;
using GetYourGigs.Api.Endpoints;
using GetYourGigs.Api.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Configuration.GetValue<bool>("SkipLocalConfig"))
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.AddHostedServices();
builder.AddMarketplaceServices();
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
