using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Services;

public sealed partial class AuthService(
    MarketplaceDb db,
    IConfiguration config,
    IEmailCodeSender emailSender,
    TimeProvider time
) : IAuthService
{
    private string CodeHash(string email, string code) =>
        Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!),
                Encoding.UTF8.GetBytes(email + ":" + code)
            )
        );

    private static string Email(string email) => email.Trim().ToLowerInvariant();

    public async Task<EmailCodeResponse> RequestCodeAsync(
        EmailCodeRequest request,
        CancellationToken ct
    )
    {
        var email = Email(request.Email);
        var now = time.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({email}, 0))",
            ct
        );
        var challenge = await db.EmailChallenges.FindAsync([email], ct);
        Require(
            challenge is null || now - challenge.SentAt > TimeSpan.FromSeconds(60),
            "Please wait a minute before requesting another code.",
            429
        );
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        challenge ??= new EmailChallenge { Email = email };
        challenge.Hash = CodeHash(email, code);
        challenge.ExpiresAt = now.AddMinutes(10);
        challenge.Attempts = 0;
        challenge.SentAt = now;
        if (db.Entry(challenge).State == EntityState.Detached)
            db.EmailChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);
        await emailSender.SendAsync(email, code, ct);
        await transaction.CommitAsync(ct);
        return new(
            "Check your email for a six-digit sign-in code.",
            emailSender.UsesDevelopmentCode ? code : null
        );
    }

    public async Task<AuthResponse> VerifyAsync(VerifyEmailRequest request, CancellationToken ct)
    {
        var email = Email(request.Email);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({email}, 0))",
            ct
        );
        var challenge = await db.EmailChallenges.FindAsync([email], ct);
        Require(
            challenge is not null
                && challenge.ExpiresAt > time.GetUtcNow()
                && challenge.Attempts < 5,
            "This code is expired or unavailable. Request a new code.",
            401
        );
        challenge!.Attempts++;
        var valid = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(challenge.Hash),
            Convert.FromHexString(CodeHash(email, request.Code))
        );
        if (!valid)
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new ApiException(401, "That code is incorrect. Please check your email.");
        }
        db.EmailChallenges.Remove(challenge);
        var account = await db
            .Accounts.Include(x => x.Profile)
            .SingleOrDefaultAsync(x => x.Email == email, ct);
        if (account is null)
        {
            account = new Account { Email = email, Name = request.Name.Trim() };
            db.Accounts.Add(account);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Issue(account);
    }

    public async Task<AuthResponse> GoogleAsync(GoogleSignInRequest request, CancellationToken ct)
    {
        var clientId = config["Google:ClientId"];
        Require(
            !string.IsNullOrWhiteSpace(clientId),
            "Google sign-in has not been configured.",
            503
        );
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId!] }
            );
        }
        catch (InvalidJwtException)
        {
            throw new ApiException(401, "Google could not verify this sign-in.");
        }
        Require(payload.EmailVerified, "Verify your email with Google first.", 401);
        var email = Email(payload.Email);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({email}, 0))",
            ct
        );
        var account = await db
            .Accounts.Include(x => x.Profile)
            .SingleOrDefaultAsync(x => x.Email == email, ct);
        if (account is null)
        {
            account = new Account { Email = email, Name = payload.Name ?? email.Split('@')[0] };
            db.Accounts.Add(account);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Issue(account);
    }

    private AuthResponse Issue(Account account)
    {
        var expires = time.GetUtcNow().AddHours(12);
        var token = new JwtSecurityToken(
            "get-your-gigs",
            "get-your-gigs-clients",
            [
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                SecurityAlgorithms.HmacSha256
            )
        );
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, account.ToResponse());
    }

    public async Task<AccountResponse> AccountAsync(Guid id, CancellationToken ct) =>
        Found(await db.Accounts.Include(x => x.Profile).SingleOrDefaultAsync(x => x.Id == id, ct))
            .ToResponse();

    public async Task<AccountResponse> ModeAsync(
        Guid id,
        UpdateModeRequest request,
        CancellationToken ct
    )
    {
        Require(Enum.IsDefined(request.Mode), "Choose Hiring or Working.");
        var account = Found(
            await db.Accounts.Include(x => x.Profile).SingleOrDefaultAsync(x => x.Id == id, ct)
        );
        account.PreferredMode = request.Mode;
        await db.SaveChangesAsync(ct);
        return account.ToResponse();
    }

    public async Task<AccountResponse> UpdateAsync(
        Guid id,
        UpdateAccountRequest request,
        CancellationToken ct
    )
    {
        var account = Found(
            await db.Accounts.Include(x => x.Profile).SingleOrDefaultAsync(x => x.Id == id, ct)
        );
        account.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);
        return account.ToResponse();
    }
}
