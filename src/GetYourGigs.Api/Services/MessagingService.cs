using GetYourGigs.Api.Data;
using GetYourGigs.Contracts;
using Microsoft.EntityFrameworkCore;
using static GetYourGigs.Api.Services.ServiceRules;

namespace GetYourGigs.Api.Services;

public interface IMessagingService
{
    Task<ThreadResponse[]> ThreadsAsync(Guid user, CancellationToken ct);
    Task<ThreadResponse> StartAsync(Guid user, StartThreadRequest request, CancellationToken ct);
    Task<MessageResponse[]> MessagesAsync(Guid user, Guid thread, CancellationToken ct);
    Task<MessageResponse> SendAsync(
        Guid user,
        Guid thread,
        SendMessageRequest request,
        CancellationToken ct
    );
    Task<UpdateResponse[]> UpdatesAsync(Guid user, CancellationToken ct);
    Task ReadUpdatesAsync(Guid user, CancellationToken ct);
}

public sealed class MessagingService(MarketplaceDb db) : IMessagingService
{
    private IQueryable<Conversation> Threads =>
        db.Conversations.Include(x => x.First).Include(x => x.Second).Include(x => x.Gig);

    public async Task<ThreadResponse[]> ThreadsAsync(Guid user, CancellationToken ct) =>
        await Threads
            .AsNoTracking()
            .Where(x => x.FirstId == user || x.SecondId == user)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new ThreadResponse(
                x.Id,
                x.FirstId == user ? x.SecondId : x.FirstId,
                x.FirstId == user ? x.Second.Name : x.First.Name,
                x.GigId,
                x.Gig == null ? null : x.Gig.Title,
                x.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault()
                    ?? "",
                x.UpdatedAt,
                x.Messages.Count(m => m.SenderId != user && !m.Read)
            ))
            .ToArrayAsync(ct);

    public async Task<ThreadResponse> StartAsync(
        Guid user,
        StartThreadRequest request,
        CancellationToken ct
    )
    {
        Require(user != request.RecipientId, "You cannot message yourself.");
        Require(
            await db.Accounts.AnyAsync(x => x.Id == request.RecipientId, ct),
            "This person was not found.",
            404
        );
        if (request.GigId is not null)
        {
            var gig = Found(await db.Gigs.FindAsync([request.GigId.Value], ct));
            Require(
                gig.OwnerId == user || gig.OwnerId == request.RecipientId,
                "The conversation must include the gig's hirer."
            );
        }
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var first = user.CompareTo(request.RecipientId) < 0 ? user : request.RecipientId;
        var second = first == user ? request.RecipientId : user;
        await db.LockAccountAsync(first, ct);
        var thread = await Threads.SingleOrDefaultAsync(
            x => x.FirstId == first && x.SecondId == second && x.GigId == request.GigId,
            ct
        );
        if (thread is null)
        {
            thread = new Conversation
            {
                FirstId = first,
                SecondId = second,
                GigId = request.GigId,
            };
            db.Conversations.Add(thread);
        }
        db.Messages.Add(
            new Message
            {
                ConversationId = thread.Id,
                SenderId = user,
                Text = request.Message.Trim(),
            }
        );
        thread.UpdatedAt = DateTimeOffset.UtcNow;
        var sender = Found(await db.Accounts.FindAsync([user], ct));
        db.Notify(
            request.RecipientId,
            "message",
            $"{sender.Name} sent you a message.",
            request.GigId
        );
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await ThreadsAsync(user, ct)).Single(x => x.Id == thread.Id);
    }

    public async Task<MessageResponse[]> MessagesAsync(Guid user, Guid thread, CancellationToken ct)
    {
        var conversation = Found(
            await db.Conversations.SingleOrDefaultAsync(
                x => x.Id == thread && (x.FirstId == user || x.SecondId == user),
                ct
            )
        );
        // Mark only the returned window read, not unseen older history.
        var messages = await db
            .Messages.Include(x => x.Sender)
            .Where(x => x.ConversationId == conversation.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToArrayAsync(ct);
        foreach (var message in messages.Where(x => x.SenderId != user))
            message.Read = true;
        await db.SaveChangesAsync(ct);
        return messages
            .Reverse()
            .Select(x => new MessageResponse(x.Id, x.SenderId, x.Sender.Name, x.Text, x.CreatedAt))
            .ToArray();
    }

    public async Task<MessageResponse> SendAsync(
        Guid user,
        Guid thread,
        SendMessageRequest request,
        CancellationToken ct
    )
    {
        var conversation = Found(
            await db.Conversations.SingleOrDefaultAsync(
                x => x.Id == thread && (x.FirstId == user || x.SecondId == user),
                ct
            )
        );
        var sender = Found(await db.Accounts.FindAsync([user], ct));
        var message = new Message
        {
            ConversationId = thread,
            SenderId = user,
            Text = request.Text.Trim(),
        };
        db.Messages.Add(message);
        conversation.UpdatedAt = message.CreatedAt;
        db.Notify(
            conversation.FirstId == user ? conversation.SecondId : conversation.FirstId,
            "message",
            $"{sender.Name} replied to your conversation.",
            conversation.GigId
        );
        await db.SaveChangesAsync(ct);
        return new(message.Id, user, sender.Name, message.Text, message.CreatedAt);
    }

    public async Task<UpdateResponse[]> UpdatesAsync(Guid user, CancellationToken ct) =>
        await db
            .Activities.AsNoTracking()
            .Where(x => x.AccountId == user)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new UpdateResponse(x.Id, x.Kind, x.Text, x.GigId, x.CreatedAt, x.Read))
            .ToArrayAsync(ct);

    public Task ReadUpdatesAsync(Guid user, CancellationToken ct) =>
        db
            .Activities.Where(x => x.AccountId == user && !x.Read)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.Read, true), ct);
}
