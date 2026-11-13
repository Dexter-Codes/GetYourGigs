using GetYourGigs.Api.Services;
using Microsoft.AspNetCore.HttpOverrides;

namespace GetYourGigs.Api.Hosting;

public static class HostedConfiguration
{
    public static bool IsSupabase(IConfiguration config) =>
        string.Equals(config["Storage:Provider"], "Supabase", StringComparison.OrdinalIgnoreCase);

    public static void AddHostedServices(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        Validate(config);
        builder.Services.AddHttpClient("email", c => c.Timeout = TimeSpan.FromSeconds(20));
        builder.Services.AddScoped<IEmailCodeSender, EmailCodeSender>();
        builder.Services.AddScoped<IImageService, ImageService>();
        if (IsSupabase(config))
            builder.Services.AddHttpClient<IImageStorage, SupabaseImageStorage>(c =>
            {
                c.BaseAddress = new Uri(config["Storage:Supabase:Url"]!.TrimEnd('/') + "/");
                c.Timeout = TimeSpan.FromSeconds(30);
            });
        else
            builder.Services.AddScoped<IImageStorage, LocalImageStorage>();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
            ConfigureForwarding(config, options)
        );
    }

    public static void ConfigureForwarding(IConfiguration config, ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        // Render owns the immediate ingress proxy. This opt-in is rejected outside
        // Render, and never accepts X-Forwarded-Host or an unlimited proxy chain.
        if (config.GetValue<bool>("Hosting:RenderProxy"))
        {
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
        }
    }

    public static void Validate(IConfiguration config)
    {
        var provider = config["Storage:Provider"] ?? "Local";
        if (!provider.Equals("Local", StringComparison.OrdinalIgnoreCase) && !IsSupabase(config))
            throw new InvalidOperationException("Storage__Provider must be Local or Supabase.");
        if (IsSupabase(config))
        {
            if (
                !Uri.TryCreate(config["Storage:Supabase:Url"], UriKind.Absolute, out var uri)
                || uri.Scheme != "https"
                || uri.AbsolutePath != "/"
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.IsNullOrEmpty(uri.Fragment)
            )
                throw new InvalidOperationException(
                    "Storage__Supabase__Url must be the HTTPS project origin."
                );
            Required(config, "Storage:Supabase:ServiceRoleKey");
            var bucket = config["Storage:Supabase:Bucket"];
            if (
                string.IsNullOrWhiteSpace(bucket)
                || bucket.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
            )
                throw new InvalidOperationException(
                    "Storage__Supabase__Bucket must be a bucket name using letters, numbers, hyphens or underscores."
                );
        }
        if (!string.IsNullOrWhiteSpace(config["Email:Resend:ApiKey"]))
            Required(config, "Email:From");
        if (config.GetValue<bool>("Hosting:RenderProxy") && !config.GetValue<bool>("RENDER"))
            throw new InvalidOperationException(
                "Hosting__RenderProxy can only be enabled inside Render's ingress network."
            );
        if (config.GetValue<bool>("RENDER"))
        {
            if (!IsSupabase(config))
                throw new InvalidOperationException(
                    "Render requires Storage__Provider=Supabase to preserve uploaded photos."
                );
            if (
                !string.IsNullOrWhiteSpace(config["Smtp:Host"])
                && string.IsNullOrWhiteSpace(config["Email:Resend:ApiKey"])
            )
                throw new InvalidOperationException(
                    "Use Email__Resend__ApiKey on Render; Free instances block the standard SMTP ports."
                );
            if (
                string.IsNullOrWhiteSpace(config["Google:ClientId"])
                && string.IsNullOrWhiteSpace(config["Email:Resend:ApiKey"])
            )
                throw new InvalidOperationException(
                    "Configure Google__ClientId or Email__Resend__ApiKey before deploying to Render."
                );
        }
    }

    private static void Required(IConfiguration config, string key)
    {
        if (string.IsNullOrWhiteSpace(config[key]))
            throw new InvalidOperationException(
                $"Set {key.Replace(":", "__")} in the host's secret settings."
            );
    }
}
