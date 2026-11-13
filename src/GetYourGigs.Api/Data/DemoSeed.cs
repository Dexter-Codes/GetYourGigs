using Microsoft.EntityFrameworkCore;

namespace GetYourGigs.Api.Data;

public static class DemoSeed
{
    public static async Task RunAsync(MarketplaceDb db)
    {
        if (await db.Accounts.AnyAsync())
            return;
        var hirer = new Account { Name = "Aanya Shah (sample)", Email = "hirer@example.test" };
        db.Accounts.Add(hirer);
        var samples = new[]
        {
            (
                "Jahan Klong",
                "Live acoustic band",
                "Music & Performance",
                "Mumbai",
                15000m,
                "Acoustic",
                "photo-1516280440614-37939bbacd81"
            ),
            (
                "Priya Sharma",
                "Wedding & portrait photographer",
                "Creative & Design",
                "Bengaluru",
                12000m,
                "Photography",
                "photo-1534528741775-53994a69daeb"
            ),
            (
                "Arjun Mehta",
                "Experienced event chef",
                "Events & Hospitality",
                "Mumbai",
                4000m,
                "Catering",
                "photo-1556157382-97eda2d62296"
            ),
            (
                "Neha Roy",
                "Brand & visual designer",
                "Creative & Design",
                "Pune",
                18000m,
                "Branding",
                "photo-1524504388940-b1c1722653e1"
            ),
            (
                "Rohan Das",
                "React & .NET developer",
                "Tech & Development",
                "Remote",
                25000m,
                "React",
                "photo-1500648767791-00dcc994a43e"
            ),
            (
                "Maya Iyer",
                "Yoga & movement coach",
                "Coaching & Wellness",
                "Mumbai",
                2000m,
                "Wellness",
                "photo-1531123897727-8f129e1688ce"
            ),
        };
        foreach (var (name, headline, category, city, rate, skill, photo) in samples)
        {
            var person = new Account
            {
                Name = name + " (sample)",
                Email = name.Split(' ')[0].ToLowerInvariant() + "@example.test",
                PreferredMode = Contracts.AccountMode.Working,
            };
            db.Accounts.Add(person);
            db.Profiles.Add(
                new TalentProfile
                {
                    OwnerId = person.Id,
                    Headline = headline,
                    Category = category,
                    City = city,
                    Rate = rate,
                    Bio =
                        "Sample talent listing for exploring Get Your Gigs. This is demonstration content, not a real person's offer of services.",
                    Skills = [skill, "Independent", "Events"],
                    Photos =
                    [
                        $"https://images.unsplash.com/{photo}?auto=format&fit=crop&w=900&q=85",
                    ],
                }
            );
        }
        var today = DateOnly.FromDateTime(
            TimeZoneInfo
                .ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Kolkata")
                .DateTime
        );
        var jobs = new[]
        {
            (
                "Live acoustic band for an intimate wedding",
                "Music & Performance",
                15000m,
                "photo-1516280440614-37939bbacd81"
            ),
            (
                "Wedding chef for a family celebration",
                "Events & Hospitality",
                8000m,
                "photo-1555939594-58d7cb561ad1"
            ),
            (
                "Brand designer for a new coffee studio",
                "Creative & Design",
                20000m,
                "photo-1495474472287-4d71bcdd2085"
            ),
            (
                "React developer for a booking dashboard",
                "Tech & Development",
                35000m,
                "photo-1498050108023-c5249f4df085"
            ),
            (
                "Yoga instructor for a community morning",
                "Coaching & Wellness",
                3000m,
                "photo-1544367567-0f2fcb009e0b"
            ),
            (
                "Event photography for a product launch",
                "Creative & Design",
                12000m,
                "photo-1516035069371-29a1b244cc32"
            ),
        };
        var index = 0;
        foreach (var (title, category, budget, photo) in jobs)
            db.Gigs.Add(
                new Gig
                {
                    OwnerId = hirer.Id,
                    Title = title + " (sample)",
                    Category = category,
                    City = "Mumbai",
                    Budget = budget,
                    Date = today.AddDays(index++),
                    Description =
                        "Sample gig for exploring the application, messaging, and booking flows. Tell us about your relevant experience and availability. This listing is demonstration content and is not a real job offer.",
                    Photos =
                    [
                        $"https://images.unsplash.com/{photo}?auto=format&fit=crop&w=1000&q=85",
                    ],
                }
            );
        await db.SaveChangesAsync();
    }
}
