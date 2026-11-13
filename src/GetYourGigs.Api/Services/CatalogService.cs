using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;
using Microsoft.EntityFrameworkCore;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Services;

public interface ICatalogService
{
    Task<PageResponse<ProfileResponse>> ProfilesAsync(
        bool signedIn,
        string? q,
        string? category,
        string? city,
        bool? available,
        int page,
        CancellationToken ct
    );
    Task<ProfileResponse> ProfileAsync(Guid id, CancellationToken ct);
    Task<ProfileResponse?> MyProfileAsync(Guid user, CancellationToken ct);
    Task<ProfileResponse> SaveProfileAsync(
        Guid user,
        SaveProfileRequest request,
        CancellationToken ct
    );
    Task<PageResponse<GigResponse>> GigsAsync(
        bool signedIn,
        string? q,
        string? category,
        string? city,
        int page,
        CancellationToken ct
    );
    Task<GigResponse> GigAsync(Guid id, CancellationToken ct);
    Task<GigResponse[]> MineAsync(Guid user, CancellationToken ct);
    Task<GigResponse> SaveGigAsync(
        Guid user,
        Guid? id,
        SaveGigRequest request,
        CancellationToken ct
    );
    Task<GigResponse> CloseAsync(Guid user, Guid id, CancellationToken ct);
    Task<BookmarkResponse> BookmarkByIdAsync(Guid user, Guid id, CancellationToken ct);
    Task<BookmarkResponse[]> BookmarksAsync(Guid user, CancellationToken ct);
    Task<BookmarkResponse> BookmarkAsync(
        Guid user,
        SaveBookmarkRequest request,
        CancellationToken ct
    );
    Task RemoveBookmarkAsync(Guid user, Guid id, CancellationToken ct);
}

public sealed class CatalogService(MarketplaceDb db, TimeProvider time) : ICatalogService
{
    private IQueryable<TalentProfile> Profiles =>
        db.Profiles.Include(x => x.Owner).Include(x => x.Reviews);
    private IQueryable<Gig> Gigs => db.Gigs.Include(x => x.Owner).Include(x => x.Applications);

    public async Task<PageResponse<ProfileResponse>> ProfilesAsync(
        bool signedIn,
        string? q,
        string? category,
        string? city,
        bool? available,
        int page,
        CancellationToken ct
    )
    {
        var query = Profiles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = "%" + q.Trim() + "%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Headline, term)
                || EF.Functions.ILike(x.Owner.Name, term)
                || EF.Functions.ILike(x.Bio, term)
            );
        }
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(x => EF.Functions.ILike(x.City, "%" + city.Trim() + "%"));
        if (available == true)
            query = query.Where(x => x.Available);
        var total = await query.CountAsync(ct);
        var size = signedIn ? 12 : 3;
        page = signedIn ? Math.Clamp(page, 1, 10000) : 1;
        var items = await query
            .OrderBy(x => x.Owner.Name)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToArrayAsync(ct);
        return new(items.Select(x => x.ToResponse()).ToArray(), total, !signedIn, page, size);
    }

    public async Task<ProfileResponse> ProfileAsync(Guid id, CancellationToken ct) =>
        Found(await Profiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)).ToResponse();

    public async Task<ProfileResponse?> MyProfileAsync(Guid user, CancellationToken ct) =>
        (
            await Profiles.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerId == user, ct)
        )?.ToResponse();

    public async Task<ProfileResponse> SaveProfileAsync(
        Guid user,
        SaveProfileRequest request,
        CancellationToken ct
    )
    {
        Category(request.Category);
        Photos(request.Photos);
        Require(
            new[] { "per gig", "per hour", "per day", "per project" }.Contains(request.RateUnit),
            "Choose a valid rate unit."
        );
        Require(
            request.Skills is null
                || (
                    request.Skills.Length <= 15
                    && request.Skills.All(x => !string.IsNullOrWhiteSpace(x) && x.Length <= 60)
                ),
            "Use up to 15 short skills."
        );
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LockAccountAsync(user, ct);
        var profile = await Profiles.SingleOrDefaultAsync(x => x.OwnerId == user, ct);
        if (profile is null)
        {
            profile = new TalentProfile
            {
                OwnerId = user,
                Owner = Found(await db.Accounts.FindAsync([user], ct)),
            };
            db.Profiles.Add(profile);
        }
        profile.Headline = request.Headline.Trim();
        profile.Category = request.Category;
        profile.City = request.City.Trim();
        profile.Rate = request.Rate;
        profile.RateUnit = request.RateUnit;
        profile.Bio = request.Bio.Trim();
        profile.Skills = request.Skills?.Select(x => x.Trim()).Distinct().ToArray() ?? [];
        profile.Photos = request.Photos ?? [];
        profile.Available = request.Available;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return profile.ToResponse();
    }

    public async Task<PageResponse<GigResponse>> GigsAsync(
        bool signedIn,
        string? q,
        string? category,
        string? city,
        int page,
        CancellationToken ct
    )
    {
        var query = Gigs.AsNoTracking()
            .Where(x => x.Status == GigStatus.Open && (x.Date == null || x.Date >= Today(time)));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = "%" + q.Trim() + "%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Title, term) || EF.Functions.ILike(x.Description, term)
            );
        }
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(x => EF.Functions.ILike(x.City, "%" + city.Trim() + "%"));
        var total = await query.CountAsync(ct);
        var size = signedIn ? 12 : 3;
        page = signedIn ? Math.Clamp(page, 1, 10000) : 1;
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToArrayAsync(ct);
        return new(items.Select(x => x.ToResponse()).ToArray(), total, !signedIn, page, size);
    }

    public async Task<GigResponse> GigAsync(Guid id, CancellationToken ct) =>
        Found(await Gigs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)).ToResponse();

    public async Task<GigResponse[]> MineAsync(Guid user, CancellationToken ct) =>
        (
            await Gigs.AsNoTracking()
                .Where(x => x.OwnerId == user)
                .OrderByDescending(x => x.CreatedAt)
                .ToArrayAsync(ct)
        )
            .Select(x => x.ToResponse())
            .ToArray();

    public async Task<GigResponse> SaveGigAsync(
        Guid user,
        Guid? id,
        SaveGigRequest request,
        CancellationToken ct
    )
    {
        Category(request.Category);
        Photos(request.Photos);
        Require(
            request.Date is null || request.Date >= Today(time),
            "Choose today or a future date."
        );
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        Gig gig;
        if (id is not null)
        {
            await db.LockGigAsync(id.Value, ct);
            gig = Found(await Gigs.SingleOrDefaultAsync(x => x.Id == id, ct));
            Own(gig.OwnerId == user);
            Require(gig.Status == GigStatus.Open, "Only open gigs can be edited.", 409);
            Require(
                gig.Applications.Count == 0 || gig.Date == request.Date,
                "Dates cannot change after people have applied. Close this gig and post a new one.",
                409
            );
        }
        else
        {
            gig = new Gig
            {
                OwnerId = user,
                Owner = Found(await db.Accounts.FindAsync([user], ct)),
            };
            db.Gigs.Add(gig);
        }
        gig.Title = request.Title.Trim();
        gig.Category = request.Category;
        gig.City = request.City.Trim();
        gig.Description = request.Description.Trim();
        gig.Date = request.Date;
        gig.Budget = request.Budget;
        gig.Photos = request.Photos ?? [];
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return gig.ToResponse();
    }

    public async Task<GigResponse> CloseAsync(Guid user, Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LockGigAsync(id, ct);
        var gig = Found(await Gigs.SingleOrDefaultAsync(x => x.Id == id, ct));
        Own(gig.OwnerId == user);
        Require(gig.Status == GigStatus.Open, "Only an open gig can be closed.", 409);
        gig.Status = GigStatus.Closed;
        foreach (
            var app in gig.Applications.Where(x =>
                x.Status
                    is ApplicationStatus.Applied
                        or ApplicationStatus.Shortlisted
                        or ApplicationStatus.Waitlisted
            )
        )
        {
            app.Status = ApplicationStatus.NotSelected;
            db.Notify(app.ApplicantId, "status", $"{gig.Title} was closed by the hirer.", id);
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return gig.ToResponse();
    }

    public async Task<BookmarkResponse> BookmarkByIdAsync(Guid user, Guid id, CancellationToken ct)
    {
        var item = Found(
            await db
                .Bookmarks.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.AccountId == user, ct)
        );
        return new(item.Id, item.TargetType, item.TargetId);
    }

    public async Task<BookmarkResponse[]> BookmarksAsync(Guid user, CancellationToken ct) =>
        await db
            .Bookmarks.AsNoTracking()
            .Where(x => x.AccountId == user)
            .Select(x => new BookmarkResponse(x.Id, x.TargetType, x.TargetId))
            .ToArrayAsync(ct);

    public async Task<BookmarkResponse> BookmarkAsync(
        Guid user,
        SaveBookmarkRequest request,
        CancellationToken ct
    )
    {
        Require(request.TargetType is "profile" or "gig", "Save a profile or gig.");
        var exists =
            request.TargetType == "profile"
                ? await db.Profiles.AnyAsync(x => x.Id == request.TargetId, ct)
                : await db.Gigs.AnyAsync(x => x.Id == request.TargetId, ct);
        Require(exists, "The listing no longer exists.", 404);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LockAccountAsync(user, ct);
        var item = await db.Bookmarks.SingleOrDefaultAsync(
            x =>
                x.AccountId == user
                && x.TargetId == request.TargetId
                && x.TargetType == request.TargetType,
            ct
        );
        if (item is null)
        {
            item = new Bookmark
            {
                AccountId = user,
                TargetId = request.TargetId,
                TargetType = request.TargetType,
            };
            db.Bookmarks.Add(item);
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return new(item.Id, item.TargetType, item.TargetId);
    }

    public async Task RemoveBookmarkAsync(Guid user, Guid id, CancellationToken ct)
    {
        var item = Found(
            await db.Bookmarks.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == user, ct)
        );
        db.Bookmarks.Remove(item);
        await db.SaveChangesAsync(ct);
    }
}
