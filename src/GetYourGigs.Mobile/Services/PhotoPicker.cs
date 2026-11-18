namespace GetYourGigs.Mobile.Services;

public sealed class PhotoPicker : IPhotoPicker
{
    public async Task<IReadOnlyList<SelectedPhoto>> PickAsync()
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync();
            return photos
                .Select(photo => new SelectedPhoto(photo.FileName, photo.OpenReadAsync))
                .ToArray();
        }
        catch (Exception error) when (error is PermissionException or FeatureNotSupportedException)
        {
            throw new IOException(
                "The photo could not be selected. You can add an HTTPS photo URL instead.",
                error
            );
        }
    }
}
