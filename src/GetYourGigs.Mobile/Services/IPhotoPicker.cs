namespace GetYourGigs.Mobile.Services;

public sealed record SelectedPhoto(string FileName, Func<Task<Stream>> OpenReadAsync);

public interface IPhotoPicker
{
    Task<IReadOnlyList<SelectedPhoto>> PickAsync();
}
