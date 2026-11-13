using Microsoft.EntityFrameworkCore;

namespace GetYourGigs.Api.Data;

public sealed class MarketplaceDb(DbContextOptions<MarketplaceDb> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<TalentProfile> Profiles => Set<TalentProfile>();
    public DbSet<Gig> Gigs => Set<Gig>();
    public DbSet<GigApplication> Applications => Set<GigApplication>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<EmailChallenge> EmailChallenges => Set<EmailChallenge>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Account>().Property(x => x.Email).HasMaxLength(254);
        b.Entity<TalentProfile>()
            .HasOne(x => x.Owner)
            .WithOne(x => x.Profile)
            .HasForeignKey<TalentProfile>(x => x.OwnerId);
        b.Entity<TalentProfile>().HasIndex(x => new { x.Category, x.City });
        b.Entity<Gig>()
            .HasIndex(x => new
            {
                x.Status,
                x.Category,
                x.City,
            });
        b.Entity<GigApplication>().HasIndex(x => new { x.GigId, x.ApplicantId }).IsUnique();
        b.Entity<GigApplication>().HasIndex(x => new { x.ApplicantId, x.ApplicationDay });
        b.Entity<GigApplication>().HasIndex(x => x.GigId).IsUnique().HasFilter("\"Status\" = 2");
        b.Entity<Bookmark>()
            .HasIndex(x => new
            {
                x.AccountId,
                x.TargetType,
                x.TargetId,
            })
            .IsUnique();
        b.Entity<Conversation>().HasOne(x => x.First).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Conversation>().HasOne(x => x.Second).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Conversation>()
            .HasIndex(x => new
            {
                x.FirstId,
                x.SecondId,
                x.GigId,
            })
            .IsUnique()
            .AreNullsDistinct(false);
        b.Entity<Conversation>()
            .ToTable(t =>
                t.HasCheckConstraint("CK_Conversation_Participants", "\"FirstId\" <> \"SecondId\"")
            );
        b.Entity<Message>().HasIndex(x => new { x.ConversationId, x.CreatedAt });
        b.Entity<Activity>().HasIndex(x => new { x.AccountId, x.CreatedAt });
        b.Entity<Review>().HasIndex(x => x.ApplicationId).IsUnique();
        b.Entity<Review>()
            .ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "\"Rating\" BETWEEN 1 AND 5"));
        b.Entity<EmailChallenge>().HasKey(x => x.Email);
        b.Entity<MobileGrant>().HasKey(x => x.Hash);
        b.Entity<MobileGrant>().HasIndex(x => x.ExpiresAt);
        foreach (var entity in b.Model.GetEntityTypes())
        foreach (
            var property in entity
                .GetProperties()
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
        )
            property.SetColumnType("numeric(14,2)");
    }

    // Lock the applicant first, then the gig, in every application mutation.
    // This serializes daily limits and cross-gig booking decisions across API instances.
    public Task LockAccountAsync(Guid id, CancellationToken ct) =>
        Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Accounts\" WHERE \"Id\" = {id} FOR UPDATE",
            ct
        );

    public Task LockGigAsync(Guid id, CancellationToken ct) =>
        Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Gigs\" WHERE \"Id\" = {id} FOR UPDATE",
            ct
        );

    public void Notify(Guid account, string kind, string text, Guid? gig = null) =>
        Activities.Add(
            new Activity
            {
                AccountId = account,
                Kind = kind,
                Text = text,
                GigId = gig,
            }
        );
}
