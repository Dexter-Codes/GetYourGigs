using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;
using Microsoft.EntityFrameworkCore;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Services;

public interface IBookingService
{
    Task<ApplicationResponse> ApplicationAsync(Guid user, Guid id, CancellationToken ct);
    Task<ReviewResponse> ReviewByIdAsync(Guid id, CancellationToken ct);
    Task<AllowanceResponse> AllowanceAsync(Guid user, CancellationToken ct);
    Task<ApplicationResponse> ApplyAsync(
        Guid user,
        Guid gigId,
        ApplyRequest request,
        CancellationToken ct
    );
    Task<ApplicationResponse[]> ApplicationsAsync(Guid user, Guid? gigId, CancellationToken ct);
    Task<ApplicationResponse> ChangeAsync(
        Guid user,
        Guid id,
        ApplicationActionRequest request,
        CancellationToken ct
    );
    Task<ApplicationResponse> CancelAsync(Guid user, Guid id, CancellationToken ct);
    Task<GigResponse> CompleteAsync(Guid user, Guid id, CancellationToken ct);
    Task<ReviewResponse> ReviewAsync(Guid user, CreateReviewRequest request, CancellationToken ct);
    Task<ReviewResponse[]> ReviewsAsync(Guid profileId, CancellationToken ct);
}

public sealed class BookingService(MarketplaceDb db, TimeProvider time) : IBookingService
{
    private IQueryable<GigApplication> Applications =>
        db
            .Applications.Include(x => x.Gig)
                .ThenInclude(x => x.Owner)
            .Include(x => x.Applicant)
                .ThenInclude(x => x.Profile);

    public async Task<ApplicationResponse> ApplicationAsync(
        Guid user,
        Guid id,
        CancellationToken ct
    ) =>
        Found(
                await Applications
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.Id == id && (x.ApplicantId == user || x.Gig.OwnerId == user),
                        ct
                    )
            )
            .ToResponse();

    public async Task<ReviewResponse> ReviewByIdAsync(Guid id, CancellationToken ct) =>
        Found(
                await db
                    .Reviews.AsNoTracking()
                    .Include(x => x.Hirer)
                    .Include(x => x.Application)
                        .ThenInclude(x => x.Gig)
                    .SingleOrDefaultAsync(x => x.Id == id, ct)
            )
            .ToResponse();

    public async Task<AllowanceResponse> AllowanceAsync(Guid user, CancellationToken ct)
    {
        var day = Today(time);
        return new(
            await db.Applications.CountAsync(
                x => x.ApplicantId == user && x.ApplicationDay == day,
                ct
            ),
            3,
            day
        );
    }

    public async Task<ApplicationResponse> ApplyAsync(
        Guid user,
        Guid gigId,
        ApplyRequest request,
        CancellationToken ct
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LockAccountAsync(user, ct);
        await db.LockGigAsync(gigId, ct);
        var gig = Found(
            await db.Gigs.Include(x => x.Owner).SingleOrDefaultAsync(x => x.Id == gigId, ct)
        );
        Require(gig.OwnerId != user, "You cannot apply to your own gig.");
        Require(
            gig.Status == GigStatus.Open && (gig.Date is null || gig.Date >= Today(time)),
            "This gig is no longer accepting applications.",
            409
        );
        Require(
            await db.Profiles.AnyAsync(x => x.OwnerId == user, ct),
            "Finish your five-question talent profile before applying.",
            409
        );
        Require(
            !await db.Applications.AnyAsync(x => x.GigId == gigId && x.ApplicantId == user, ct),
            "You have already applied to this gig.",
            409
        );
        var allowance = await AllowanceAsync(user, ct);
        Require(
            allowance.Used < allowance.Limit,
            "You have used your three applications for today. Come back tomorrow.",
            429
        );
        if (gig.Date is not null)
            Require(
                !await db.Applications.AnyAsync(
                    x =>
                        x.ApplicantId == user
                        && x.Status == ApplicationStatus.Booked
                        && x.Gig.Date == gig.Date,
                    ct
                ),
                "You are already booked on this date.",
                409
            );
        var app = new GigApplication
        {
            GigId = gigId,
            Gig = gig,
            ApplicantId = user,
            Applicant = Found(
                await db
                    .Accounts.Include(x => x.Profile)
                    .SingleOrDefaultAsync(x => x.Id == user, ct)
            ),
            Message = request.Message.Trim(),
            ExpectedPrice = request.ExpectedPrice,
            ApplicationDay = Today(time),
        };
        db.Applications.Add(app);
        db.Notify(
            gig.OwnerId,
            "application",
            $"{app.Applicant.Name} applied to {gig.Title}.",
            gig.Id
        );
        db.Notify(user, "application", $"Your application to {gig.Title} was sent.", gig.Id);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return app.ToResponse();
    }

    public async Task<ApplicationResponse[]> ApplicationsAsync(
        Guid user,
        Guid? gigId,
        CancellationToken ct
    )
    {
        var query = Applications.AsNoTracking();
        if (gigId is not null)
        {
            var gig = Found(await db.Gigs.FindAsync([gigId.Value], ct));
            Own(gig.OwnerId == user);
            query = query.Where(x => x.GigId == gigId);
        }
        else
            query = query.Where(x => x.ApplicantId == user);
        return (await query.OrderByDescending(x => x.CreatedAt).ToArrayAsync(ct))
            .Select(x => x.ToResponse())
            .ToArray();
    }

    public async Task<ApplicationResponse> ChangeAsync(
        Guid user,
        Guid id,
        ApplicationActionRequest request,
        CancellationToken ct
    )
    {
        Require(
            request.Status
                is ApplicationStatus.Shortlisted
                    or ApplicationStatus.Booked
                    or ApplicationStatus.Declined,
            "Choose shortlist, book, or decline."
        );
        var pointer = Found(
            await db.Applications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
        );
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LockAccountAsync(pointer.ApplicantId, ct);
        await db.LockGigAsync(pointer.GigId, ct);
        var app = Found(await Applications.SingleOrDefaultAsync(x => x.Id == id, ct));
        Own(app.Gig.OwnerId == user);
        Require(app.Gig.Status == GigStatus.Open, "This gig is no longer open.", 409);
        Require(
            app.Status
                is ApplicationStatus.Applied
                    or ApplicationStatus.Shortlisted
                    or ApplicationStatus.Waitlisted,
            "This application can no longer change.",
            409
        );
        if (request.Status == ApplicationStatus.Booked)
        {
            Require(
                app.Gig.Date is null || app.Gig.Date >= Today(time),
                "You cannot book a gig in the past.",
                409
            );
            if (app.Gig.Date is not null)
            {
                Require(
                    !await db.Applications.AnyAsync(
                        x =>
                            x.ApplicantId == app.ApplicantId
                            && x.Id != id
                            && x.Status == ApplicationStatus.Booked
                            && x.Gig.Date == app.Gig.Date,
                        ct
                    ),
                    "This talent is already booked on that date.",
                    409
                );
                var conflicts = await db
                    .Applications.Include(x => x.Gig)
                    .Where(x =>
                        x.ApplicantId == app.ApplicantId
                        && x.GigId != app.GigId
                        && x.Gig.Date == app.Gig.Date
                        && (
                            x.Status == ApplicationStatus.Applied
                            || x.Status == ApplicationStatus.Shortlisted
                            || x.Status == ApplicationStatus.Waitlisted
                        )
                    )
                    .ToArrayAsync(ct);
                foreach (var other in conflicts)
                {
                    other.Status = ApplicationStatus.Cancelled;
                    db.Notify(
                        other.Gig.OwnerId,
                        "status",
                        $"{app.Applicant.Name}'s application to {other.Gig.Title} was cancelled because they were booked elsewhere on the same date.",
                        other.GigId
                    );
                    db.Notify(
                        app.ApplicantId,
                        "status",
                        $"Your application to {other.Gig.Title} was cancelled to avoid a date conflict.",
                        other.GigId
                    );
                }
            }
            var siblings = await db
                .Applications.Where(x =>
                    x.GigId == app.GigId
                    && x.Id != id
                    && (
                        x.Status == ApplicationStatus.Applied
                        || x.Status == ApplicationStatus.Shortlisted
                        || x.Status == ApplicationStatus.Waitlisted
                    )
                )
                .ToArrayAsync(ct);
            foreach (var other in siblings)
            {
                other.Status = ApplicationStatus.Waitlisted;
                db.Notify(
                    other.ApplicantId,
                    "status",
                    $"You are on the waitlist for {app.Gig.Title}.",
                    app.GigId
                );
            }
            app.Gig.Status = GigStatus.Booked;
        }
        app.Status = request.Status;
        db.Notify(
            app.ApplicantId,
            "status",
            $"Your application to {app.Gig.Title} is now {request.Status.ToString().ToLowerInvariant()}.",
            app.GigId
        );
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return app.ToResponse();
    }

    public async Task<ApplicationResponse> CancelAsync(Guid user, Guid id, CancellationToken ct)
    {
        var pointer = Found(
            await db.Applications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
        );
        Own(pointer.ApplicantId == user);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LockAccountAsync(user, ct);
        await db.LockGigAsync(pointer.GigId, ct);
        var app = Found(await Applications.SingleOrDefaultAsync(x => x.Id == id, ct));
        Require(
            app.Status
                is ApplicationStatus.Applied
                    or ApplicationStatus.Shortlisted
                    or ApplicationStatus.Waitlisted
                    or ApplicationStatus.Booked,
            "This application cannot be withdrawn.",
            409
        );
        if (app.Status == ApplicationStatus.Booked)
        {
            Require(
                app.Gig.Date is null || app.Gig.Date >= Today(time),
                "Contact the hirer about a past booking.",
                409
            );
            app.Gig.Status = GigStatus.Open;
        }
        app.Status = ApplicationStatus.Cancelled;
        db.Notify(
            app.Gig.OwnerId,
            "status",
            $"{app.Applicant.Name} withdrew from {app.Gig.Title}.",
            app.GigId
        );
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return app.ToResponse();
    }

    public async Task<GigResponse> CompleteAsync(Guid user, Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LockGigAsync(id, ct);
        var gig = Found(
            await db
                .Gigs.Include(x => x.Owner)
                .Include(x => x.Applications)
                .SingleOrDefaultAsync(x => x.Id == id, ct)
        );
        Own(gig.OwnerId == user);
        Require(
            gig.Status == GigStatus.Booked,
            "A booked gig must take place before it can be completed.",
            409
        );
        Require(
            gig.Date is null || gig.Date <= Today(time),
            "This gig is scheduled for a future date.",
            409
        );
        gig.Status = GigStatus.Completed;
        foreach (var app in gig.Applications)
        {
            if (app.Status == ApplicationStatus.Booked)
            {
                app.Status = ApplicationStatus.Completed;
                db.Notify(
                    app.ApplicantId,
                    "completed",
                    $"{gig.Title} was marked complete. Thank you for your work!",
                    id
                );
            }
            else if (
                app.Status
                is ApplicationStatus.Applied
                    or ApplicationStatus.Shortlisted
                    or ApplicationStatus.Waitlisted
            )
                app.Status = ApplicationStatus.NotSelected;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return gig.ToResponse();
    }

    public async Task<ReviewResponse> ReviewAsync(
        Guid user,
        CreateReviewRequest request,
        CancellationToken ct
    )
    {
        var pointer = Found(
            await db
                .Applications.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == request.ApplicationId, ct)
        );
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LockGigAsync(pointer.GigId, ct);
        var app = Found(
            await Applications.SingleOrDefaultAsync(x => x.Id == request.ApplicationId, ct)
        );
        Own(app.Gig.OwnerId == user);
        Require(
            app.Status == ApplicationStatus.Completed && app.Gig.Status == GigStatus.Completed,
            "Reviews are available only after the gig is completed.",
            409
        );
        Require(
            !await db.Reviews.AnyAsync(x => x.ApplicationId == app.Id, ct),
            "You already reviewed this gig.",
            409
        );
        var profile = Found(app.Applicant.Profile);
        var review = new Review
        {
            ApplicationId = app.Id,
            Application = app,
            ProfileId = profile.Id,
            HirerId = user,
            Hirer = app.Gig.Owner,
            Rating = request.Rating,
            Text = request.Text.Trim(),
        };
        db.Reviews.Add(review);
        db.Notify(
            app.ApplicantId,
            "review",
            $"{app.Gig.Owner.Name} reviewed your work on {app.Gig.Title}.",
            app.GigId
        );
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return review.ToResponse();
    }

    public async Task<ReviewResponse[]> ReviewsAsync(Guid profileId, CancellationToken ct) =>
        (
            await db
                .Reviews.AsNoTracking()
                .Include(x => x.Hirer)
                .Include(x => x.Application)
                    .ThenInclude(x => x.Gig)
                .Where(x => x.ProfileId == profileId)
                .OrderByDescending(x => x.CreatedAt)
                .ToArrayAsync(ct)
        )
            .Select(x => x.ToResponse())
            .ToArray();
}
