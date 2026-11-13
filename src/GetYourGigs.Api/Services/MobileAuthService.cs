using System.Security.Cryptography;
using System.Text;
using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Services;

public sealed partial class AuthService
{
    public async Task<MobileGrantResponse> MobileGrantAsync(
        Guid user,
        MobileGrantRequest request,
        CancellationToken ct
    )
    {
        var code = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        await db.Set<MobileGrant>()
            .Where(x => x.ExpiresAt < time.GetUtcNow())
            .ExecuteDeleteAsync(ct);
        db.Set<MobileGrant>()
            .Add(
                new MobileGrant
                {
                    Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))),
                    AccountId = user,
                    Challenge = request.Challenge,
                    ExpiresAt = time.GetUtcNow().AddMinutes(1),
                }
            );
        await db.SaveChangesAsync(ct);
        return new(code);
    }

    public async Task<AuthResponse> ExchangeAsync(
        MobileExchangeRequest request,
        CancellationToken ct
    )
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Code)));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"MobileGrant\" WHERE \"Hash\"={hash} FOR UPDATE",
            ct
        );
        var grant = await db.Set<MobileGrant>().FindAsync([hash], ct);
        Require(
            grant is not null && grant.ExpiresAt > time.GetUtcNow(),
            "This mobile sign-in has expired. Please try again.",
            401
        );
        var challenge = Base64UrlEncoder.Encode(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Verifier))
        );
        Require(
            CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(challenge),
                Encoding.ASCII.GetBytes(grant!.Challenge)
            ),
            "Mobile sign-in could not be verified.",
            401
        );
        var account = Found(
            await db
                .Accounts.Include(x => x.Profile)
                .SingleOrDefaultAsync(x => x.Id == grant.AccountId, ct)
        );
        db.Set<MobileGrant>().Remove(grant);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Issue(account);
    }
}
