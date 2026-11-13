using GetYourGigs.Mobile.ViewModels;

namespace GetYourGigs.Mobile.Views;

public partial class PhotoEditorView : ContentView
{
    public PhotoEditorView() => InitializeComponent();

    private async void OnPickPhoto(object? sender, EventArgs e)
    {
        if (BindingContext is not MediaEditorViewModel vm)
            return;
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync();
            foreach (var photo in photos.Take(Math.Max(0, 5 - vm.Photos.Count)))
            {
                await using var stream = await photo.OpenReadAsync();
                await vm.AddPhotoAsync(stream, photo.FileName);
            }
        }
        catch (Exception error)
            when (error is PermissionException or FeatureNotSupportedException or IOException)
        {
            vm.Error = "The photo could not be selected. You can add an HTTPS photo URL instead.";
        }
    }
}
