using System.Collections.ObjectModel;

namespace GetYourGigs.Mobile.ViewModels;

public partial class ThreadViewModel(GigsApiClient api, AppSession session) : ViewModelBase
{
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private CancellationTokenSource? pollingCancellation;

    public ThreadResponse? Thread { get; set; }
    public string Heading => Thread?.OtherName.Replace(" (sample)", "") ?? "Conversation";
    public string Subtitle => Thread?.GigTitle ?? "Good things start with hello.";

    [ObservableProperty]
    private ObservableCollection<ChatMessage> messages = [];

    [ObservableProperty]
    private string text = "";

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task AppearAsync()
    {
        pollingCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        pollingCancellation = cancellation;
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                await ExecuteAsync(
                    () => RefreshMessagesAsync(cancellation.Token),
                    cancellation.Token
                );
                await Task.Delay(TimeSpan.FromSeconds(10), cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Normal navigation away from this conversation.
        }
        finally
        {
            if (ReferenceEquals(pollingCancellation, cancellation))
                pollingCancellation = null;
        }
    }

    [RelayCommand]
    private void Disappear() => pollingCancellation?.Cancel();

    private async Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default
    )
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            await RunAsync(action, cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task RefreshMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (Thread is null)
            return;
        var response =
            await api.GetAsync<MessageResponse[]>(
                $"api/threads/{Thread.Id}/messages",
                cancellationToken
            ) ?? [];
        cancellationToken.ThrowIfCancellationRequested();
        Messages = new(
            response.Select(message => new ChatMessage(
                message.Text,
                message.CreatedAt.ToLocalTime().ToString("dd MMM · HH:mm"),
                message.SenderId == session.Account?.Id
            ))
        );
        OnPropertyChanged(nameof(Heading));
        OnPropertyChanged(nameof(Subtitle));
    }

    [RelayCommand]
    private Task SendAsync() =>
        ExecuteAsync(async () =>
        {
            if (Thread is null)
                return;
            var request = new SendMessageRequest(Text);
            Validate(request);
            await api.SendAsync<MessageResponse>($"api/threads/{Thread.Id}/messages", request);
            Text = "";
            await RefreshMessagesAsync();
        });
}
