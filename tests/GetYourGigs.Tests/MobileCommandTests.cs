using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Tests;

public sealed class MobileCommandTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    public async Task Booking_requires_confirmation_before_mutating_and_refreshing(
        bool confirmed,
        int requests
    )
    {
        var application = Application();
        using var handler = new StubHandler(request =>
            request.Method == HttpMethod.Put
                ? Json(application with { Status = ApplicationStatus.Booked })
                : Json(new[] { application with { Status = ApplicationStatus.Booked } })
        );
        using var http = Http(handler);
        var viewModel = new ApplicantsViewModel(
            new GigsApiClient(http, new Tokens()),
            new NavigationStub(),
            new Dialogs(confirmed)
        )
        {
            Gig = Gig(application.GigId),
        };

        await viewModel.BookCommand.ExecuteAsync(application);

        Assert.Equal(requests, handler.Requests);
        Assert.False(viewModel.IsBusy);
        if (confirmed)
            Assert.Equal(ApplicationStatus.Booked, Assert.Single(viewModel.Items).Status);
    }

    [Fact]
    public async Task Application_details_refresh_status_instead_of_reusing_navigation_snapshot()
    {
        var application = Application();
        using var handler = new StubHandler(request =>
            request.RequestUri!.AbsolutePath.StartsWith("/api/applications/")
                ? Json(application with { Status = ApplicationStatus.Cancelled })
                : Json(Gig(application.GigId))
        );
        using var http = Http(handler);
        var tokens = new Tokens();
        var api = new GigsApiClient(http, tokens);
        var viewModel = new DetailViewModel(
            api,
            new AppSession(api, tokens),
            new NavigationStub(),
            new Dialogs(false)
        )
        {
            Item = ListingItem.From(application),
        };

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Cancelled", viewModel.Status);
        Assert.False(viewModel.ShowWithdraw);
        Assert.Equal(2, handler.Requests);
    }

    [Fact]
    public async Task Photo_selection_uploads_only_remaining_slots_and_disposes_streams()
    {
        using var handler = new StubHandler(_ => Json(new UploadResponse("/uploads/photo.png")));
        using var http = Http(handler);
        var streams = new List<MemoryStream>();
        var picker = new PhotoPickerStub(
            Enumerable
                .Range(0, 3)
                .Select(index => new SelectedPhoto(
                    $"photo{index}.png",
                    () =>
                    {
                        var stream = new MemoryStream([1, 2, 3]);
                        streams.Add(stream);
                        return Task.FromResult<Stream>(stream);
                    }
                ))
                .ToArray()
        );
        var viewModel = new PhotoDraft(new GigsApiClient(http, new Tokens()), picker);
        viewModel.Photos = new(["a", "b", "c", "d"]);

        await viewModel.PickPhotoCommand.ExecuteAsync(null);

        Assert.Equal(5, viewModel.Photos.Count);
        Assert.Equal(1, handler.Requests);
        Assert.False(Assert.Single(streams).CanRead);
    }

    [Fact]
    public async Task Expected_navigation_cancellation_does_not_display_an_error()
    {
        var viewModel = new SimpleViewModel();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await viewModel.RunAsync(() => Task.FromCanceled(cancellation.Token), cancellation.Token);

        Assert.Empty(viewModel.Error);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Leaving_chat_cancels_inflight_refresh_and_allows_reopening()
    {
        using var handler = new BlockingHandler();
        using var http = Http(handler);
        var tokens = new Tokens();
        var api = new GigsApiClient(http, tokens);
        var viewModel = new ThreadViewModel(api, new AppSession(api, tokens))
        {
            Thread = new ThreadResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Test user",
                null,
                null,
                "",
                DateTimeOffset.UtcNow,
                0
            ),
        };

        var polling = viewModel.AppearCommand.ExecuteAsync(null);
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        viewModel.DisappearCommand.Execute(null);
        await polling.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(viewModel.IsBusy);
        Assert.Empty(viewModel.Error);
        handler.Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        polling = viewModel.AppearCommand.ExecuteAsync(null);
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        viewModel.DisappearCommand.Execute(null);
        await polling.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, handler.Requests);
    }

    private sealed class BlockingHandler : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Requests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct
        )
        {
            Requests++;
            Started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return Json(Array.Empty<MessageResponse>());
        }
    }

    private static ApplicationResponse Application() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Sample test gig",
            null,
            Guid.NewGuid(),
            "Test applicant",
            null,
            "Sample application message",
            null,
            ApplicationStatus.Applied,
            DateTimeOffset.UtcNow
        );

    private static GigResponse Gig(Guid id) =>
        new(
            id,
            Guid.NewGuid(),
            "Test owner",
            "Sample test gig",
            "Music & Performance",
            "Test city",
            "Sample description",
            null,
            100,
            GigStatus.Open,
            [],
            1,
            DateTimeOffset.UtcNow
        );

    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: JsonOptions) };

    private static HttpClient Http(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://api.example/") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response)
        : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct
        )
        {
            Requests++;
            return Task.FromResult(response(request));
        }
    }

    private sealed class Tokens : ITokenStore
    {
        public Task<string?> GetAsync() => Task.FromResult<string?>(null);

        public Task SetAsync(string? token) => Task.CompletedTask;
    }

    private sealed class Dialogs(bool confirmed) : IUserDialogs
    {
        public Task<bool> ConfirmAsync(
            string title,
            string message,
            string accept,
            string cancel
        ) => Task.FromResult(confirmed);
    }

    private sealed class PhotoPickerStub(IReadOnlyList<SelectedPhoto> photos) : IPhotoPicker
    {
        public Task<IReadOnlyList<SelectedPhoto>> PickAsync() => Task.FromResult(photos);
    }

    private sealed partial class PhotoDraft(GigsApiClient api, IPhotoPicker picker)
        : MediaEditorViewModel(api, picker);

    private sealed partial class SimpleViewModel : ViewModelBase;

    private sealed class NavigationStub : INavigator
    {
        public Task BackAsync() => Task.CompletedTask;

        public Task SignInAsync() => Task.CompletedTask;

        public Task DetailAsync(ListingItem item) => Task.CompletedTask;

        public Task ThreadAsync(ThreadResponse thread) => Task.CompletedTask;

        public Task ProfileAsync() => Task.CompletedTask;

        public Task PostAsync(GigResponse? existing = null) => Task.CompletedTask;

        public Task AccountAsync() => Task.CompletedTask;

        public Task ApplicantsAsync(GigResponse gig) => Task.CompletedTask;

        public Task ComposeAsync(
            string kind,
            Guid recipient,
            Guid? gig,
            string title,
            Guid? application = null
        ) => Task.CompletedTask;
    }
}
