using System.Net.Http.Headers;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Services;

public interface IEmailCodeSender
{
    bool UsesDevelopmentCode { get; }
    Task SendAsync(string email, string code, CancellationToken ct);
}

public sealed class EmailCodeSender(
    IConfiguration config,
    IHostEnvironment environment,
    IHttpClientFactory clients
) : IEmailCodeSender
{
    public bool UsesDevelopmentCode =>
        environment.IsDevelopment()
        && string.IsNullOrWhiteSpace(config["Email:Resend:ApiKey"])
        && string.IsNullOrWhiteSpace(config["Smtp:Host"]);

    public async Task SendAsync(string email, string code, CancellationToken ct)
    {
        var text =
            $"Your sign-in code is {code}. It expires in 10 minutes. If you didn't request it, ignore this email.";
        if (!string.IsNullOrWhiteSpace(config["Email:Resend:ApiKey"]))
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.resend.com/emails"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                config["Email:Resend:ApiKey"]
            );
            request.Content = JsonContent.Create(
                new
                {
                    from = config["Email:From"],
                    to = new[] { email },
                    subject = "Your Get Your Gigs sign-in code",
                    text,
                }
            );
            try
            {
                using var response = await clients.CreateClient("email").SendAsync(request, ct);
                Require(
                    response.IsSuccessStatusCode,
                    "Email delivery is unavailable. Please try again.",
                    503
                );
            }
            catch (Exception e)
                when (e is HttpRequestException
                    || e is OperationCanceledException && !ct.IsCancellationRequested
                )
            {
                throw new ApiException(503, "Email delivery is unavailable. Please try again.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(config["Smtp:Host"]))
        {
            var mail = new MimeMessage();
            mail.From.Add(
                MailboxAddress.Parse(
                    config["Smtp:From"] ?? "Get Your Gigs <hello@getyourgigs.example>"
                )
            );
            mail.To.Add(MailboxAddress.Parse(email));
            mail.Subject = "Your Get Your Gigs sign-in code";
            mail.Body = new TextPart("plain") { Text = text };
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                config["Smtp:Host"]!,
                config.GetValue("Smtp:Port", 587),
                SecureSocketOptions.StartTls,
                ct
            );
            if (!string.IsNullOrWhiteSpace(config["Smtp:User"]))
                await smtp.AuthenticateAsync(
                    config["Smtp:User"]!,
                    config["Smtp:Password"] ?? "",
                    ct
                );
            await smtp.SendAsync(mail, ct);
            await smtp.DisconnectAsync(true, ct);
        }
        else
            Require(
                UsesDevelopmentCode,
                "Email delivery has not been configured. Use Google sign-in or contact the administrator.",
                503
            );
    }
}
