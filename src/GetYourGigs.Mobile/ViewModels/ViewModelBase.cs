using System.ComponentModel.DataAnnotations;

namespace GetYourGigs.Mobile.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string error = "";

    public async Task RunAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        Error = "";
        try
        {
            await action();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Leaving a page cancels its work without displaying a connection error.
        }
        catch (Exception e)
            when (e
                    is ApiRequestException
                        or HttpRequestException
                        or OperationCanceledException
                        or IOException
                        or ValidationException
                        or ArgumentException
            )
        {
            Error =
                e is OperationCanceledException
                    ? "The server is taking longer than expected to wake up. Please try again shortly."
                : e is HttpRequestException
                    ? "Cannot reach the marketplace. Check your connection and API address in Profile."
                : e.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected static void Validate(object request) =>
        Validator.ValidateObject(request, new ValidationContext(request), true);
}
