using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;

namespace GetYourGigs.Api.Services;

internal static class Mapping
{
    public static AccountResponse ToResponse(this Account x) =>
        new(x.Id, x.Name, x.Email, x.PreferredMode, x.Profile is not null);

    public static ProfileResponse ToResponse(this TalentProfile x) =>
        new(
            x.Id,
            x.OwnerId,
            x.Owner.Name,
            x.Headline,
            x.Category,
            x.City,
            x.Rate,
            x.RateUnit,
            x.Bio,
            x.Skills,
            x.Photos,
            x.Available,
            x.Reviews.Count == 0 ? null : x.Reviews.Average(r => (double)r.Rating),
            x.Reviews.Count
        );

    public static GigResponse ToResponse(this Gig x) =>
        new(
            x.Id,
            x.OwnerId,
            x.Owner.Name,
            x.Title,
            x.Category,
            x.City,
            x.Description,
            x.Date,
            x.Budget,
            x.Status,
            x.Photos,
            x.Applications.Count,
            x.CreatedAt
        );

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
