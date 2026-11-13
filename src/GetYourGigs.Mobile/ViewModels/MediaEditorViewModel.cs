using System.Collections.ObjectModel;

namespace GetYourGigs.Mobile.ViewModels;

/// <summary>Shared photo draft and upload behavior for talent and gig editors.</summary>
public abstract partial class MediaEditorViewModel(GigsApiClient api) : ViewModelBase
{
    protected GigsApiClient Api { get; } = api;

    [ObservableProperty]
    private ObservableCollection<string> photos = [];

    [ObservableProperty]
    private string photoUrl = "";

    public Task AddPhotoAsync(Stream stream, string filename) =>
        RunAsync(async () =>
        {
            if (Photos.Count >= 5)
                throw new ArgumentException("Use up to five photos.");
            var uploaded = await Api.UploadAsync(stream, filename);
            Photos.Add(uploaded.Url);
        });

    [RelayCommand]
    private void AddUrl()
    {
        if (Photos.Count >= 5)
        {
            Error = "Use up to five photos.";
            return;
        }
        if (!Uri.TryCreate(PhotoUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
        {
            Error = "Use an HTTPS image URL.";
            return;
        }
        Photos.Add(PhotoUrl);
        PhotoUrl = "";
    }

    [RelayCommand]
    private void RemovePhoto(string url) => Photos.Remove(url);
}
