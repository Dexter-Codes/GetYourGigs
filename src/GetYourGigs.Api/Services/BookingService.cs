using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;
using Microsoft.EntityFrameworkCore;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Services;

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
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
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
        var application = new GigApplication
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
        db.Applications.Add(application);
        db.Notify(
            gig.OwnerId,
            "application",
            $"{application.Applicant.Name} applied to {gig.Title}.",
            gig.Id
        );
        db.Notify(user, "application", $"Your application to {gig.Title} was sent.", gig.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return application.ToResponse();
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
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.LockAccountAsync(pointer.ApplicantId, ct);
        await db.LockGigAsync(pointer.GigId, ct);
        var application = Found(await Applications.SingleOrDefaultAsync(x => x.Id == id, ct));
        Own(application.Gig.OwnerId == user);
        Require(application.Gig.Status == GigStatus.Open, "This gig is no longer open.", 409);
        Require(
            application.Status
                is ApplicationStatus.Applied
                    or ApplicationStatus.Shortlisted
                    or ApplicationStatus.Waitlisted,
            "This application can no longer change.",
            409
        );
        if (request.Status == ApplicationStatus.Booked)
            await BookAsync(application, ct);
        application.Status = request.Status;
        db.Notify(
            application.ApplicantId,
            "status",
            $"Your application to {application.Gig.Title} is now {request.Status.ToString().ToLowerInvariant()}.",
            application.GigId
        );
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return application.ToResponse();
    }

    // The caller holds the applicant lock, then the gig lock, in one transaction.
    // All conflict cancellations, waitlist changes, and notifications commit together.
    private async Task BookAsync(GigApplication application, CancellationToken ct)
    {
        Require(
            application.Gig.Date is null || application.Gig.Date >= Today(time),
            "You cannot book a gig in the past.",
            409
        );
        if (application.Gig.Date is not null)
        {
            Require(
                !await db.Applications.AnyAsync(
                    x =>
                        x.ApplicantId == application.ApplicantId
                        && x.Id != application.Id
                        && x.Status == ApplicationStatus.Booked
                        && x.Gig.Date == application.Gig.Date,
                    ct
                ),
                "This talent is already booked on that date.",
                409
            );
            var conflicts = await db
                .Applications.Include(x => x.Gig)
                .Where(x =>
                    x.ApplicantId == application.ApplicantId
                    && x.GigId != application.GigId
                    && x.Gig.Date == application.Gig.Date
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
                    $"{application.Applicant.Name}'s application to {other.Gig.Title} was cancelled because they were booked elsewhere on the same date.",
                    other.GigId
                );
                db.Notify(
                    application.ApplicantId,
                    "status",
                    $"Your application to {other.Gig.Title} was cancelled to avoid a date conflict.",
                    other.GigId
                );
            }
        }
        var siblings = await db
            .Applications.Where(x =>
                x.GigId == application.GigId
                && x.Id != application.Id
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
                $"You are on the waitlist for {application.Gig.Title}.",
                application.GigId
            );
        }
        application.Gig.Status = GigStatus.Booked;
    }

    public async Task<ApplicationResponse> CancelAsync(Guid user, Guid id, CancellationToken ct)
    {
        var pointer = Found(
            await db.Applications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
        );
        Own(pointer.ApplicantId == user);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.LockAccountAsync(user, ct);
        await db.LockGigAsync(pointer.GigId, ct);
        var application = Found(await Applications.SingleOrDefaultAsync(x => x.Id == id, ct));
        Require(
            application.Status
                is ApplicationStatus.Applied
                    or ApplicationStatus.Shortlisted
                    or ApplicationStatus.Waitlisted
                    or ApplicationStatus.Booked,
            "This application cannot be withdrawn.",
            409
        );
        if (application.Status == ApplicationStatus.Booked)
        {
            Require(
                application.Gig.Date is null || application.Gig.Date >= Today(time),
                "Contact the hirer about a past booking.",
                409
            );
            application.Gig.Status = GigStatus.Open;
        }
        application.Status = ApplicationStatus.Cancelled;
        db.Notify(
            application.Gig.OwnerId,
            "status",
            $"{application.Applicant.Name} withdrew from {application.Gig.Title}.",
            application.GigId
        );
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return application.ToResponse();
    }

    public async Task<GigResponse> CompleteAsync(Guid user, Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
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
        foreach (var application in gig.Applications)
        {
            if (application.Status == ApplicationStatus.Booked)
            {
                application.Status = ApplicationStatus.Completed;
                db.Notify(
                    application.ApplicantId,
                    "completed",
                    $"{gig.Title} was marked complete. Thank you for your work!",
                    id
                );
            }
            else if (
                application.Status
                is ApplicationStatus.Applied
                    or ApplicationStatus.Shortlisted
                    or ApplicationStatus.Waitlisted
            )
                application.Status = ApplicationStatus.NotSelected;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
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
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.LockGigAsync(pointer.GigId, ct);
        var application = Found(
            await Applications.SingleOrDefaultAsync(x => x.Id == request.ApplicationId, ct)
        );
        Own(application.Gig.OwnerId == user);
        Require(
            application.Status == ApplicationStatus.Completed
                && application.Gig.Status == GigStatus.Completed,
            "Reviews are available only after the gig is completed.",
            409
        );
        Require(
            !await db.Reviews.AnyAsync(x => x.ApplicationId == application.Id, ct),
            "You already reviewed this gig.",
            409
        );
        var profile = Found(application.Applicant.Profile);
        var review = new Review
        {
            ApplicationId = application.Id,
            Application = application,
            ProfileId = profile.Id,
            HirerId = user,
            Hirer = application.Gig.Owner,
            Rating = request.Rating,
            Text = request.Text.Trim(),
        };
        db.Reviews.Add(review);
        db.Notify(
            application.ApplicantId,
            "review",
            $"{application.Gig.Owner.Name} reviewed your work on {application.Gig.Title}.",
            application.GigId
        );
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
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
