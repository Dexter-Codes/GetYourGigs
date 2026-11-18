using System.Linq.Expressions;
using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;

namespace GetYourGigs.Api.Services;

internal static class Mapping
{
    public static AccountResponse ToResponse(this Account x) =>
        new(x.Id, x.Name, x.Email, x.PreferredMode, x.Profile is not null);

    public static readonly Expression<Func<TalentProfile, ProfileResponse>> ProfileProjection =
        profile => new ProfileResponse(
            profile.Id,
            profile.OwnerId,
            profile.Owner.Name,
            profile.Headline,
            profile.Category,
            profile.City,
            profile.Rate,
            profile.RateUnit,
            profile.Bio,
            profile.Skills,
            profile.Photos,
            profile.Available,
            profile.Reviews.Count == 0 ? null : profile.Reviews.Average(r => (double)r.Rating),
            profile.Reviews.Count
        );

    public static readonly Expression<Func<Gig, GigResponse>> GigProjection =
        gig => new GigResponse(
            gig.Id,
            gig.OwnerId,
            gig.Owner.Name,
            gig.Title,
            gig.Category,
            gig.City,
            gig.Description,
            gig.Date,
            gig.Budget,
            gig.Status,
            gig.Photos,
            gig.Applications.Count,
            gig.CreatedAt
        );

    // Mutation responses map the already-loaded tracked graph; reads use the same
    // expressions in SQL so counts and averages do not load related collections.
    private static readonly Func<TalentProfile, ProfileResponse> MapProfile =
        ProfileProjection.Compile();
    private static readonly Func<Gig, GigResponse> MapGig = GigProjection.Compile();

    public static ProfileResponse ToResponse(this TalentProfile profile) => MapProfile(profile);

    public static GigResponse ToResponse(this Gig gig) => MapGig(gig);

    public static ApplicationResponse ToResponse(this GigApplication x) =>
        new(
            x.Id,
            x.GigId,
            x.Gig.Title,
            x.Gig.Date,
            x.ApplicantId,
            x.Applicant.Name,
            x.Applicant.Profile?.Id,
            x.Message,
            x.ExpectedPrice,
            x.Status,
            x.CreatedAt
        );

    public static ReviewResponse ToResponse(this Review x) =>
        new(x.Id, x.Hirer.Name, x.Application.Gig.Title, x.Rating, x.Text, x.CreatedAt);
}
