using System.Windows.Input;

namespace GetYourGigs.Mobile.Behaviors;

/// <summary>Connects page visibility to view-model commands without page event handlers.</summary>
public static class PageCommands
{
    public static readonly BindableProperty AppearingProperty = BindableProperty.CreateAttached(
        "Appearing",
        typeof(ICommand),
        typeof(PageCommands),
        null,
        propertyChanged: OnAppearingChanged
    );

    public static readonly BindableProperty DisappearingProperty = BindableProperty.CreateAttached(
        "Disappearing",
        typeof(ICommand),
        typeof(PageCommands),
        null,
        propertyChanged: OnDisappearingChanged
    );

    public static ICommand? GetAppearing(BindableObject view) =>
        (ICommand?)view.GetValue(AppearingProperty);

    public static void SetAppearing(BindableObject view, ICommand? value) =>
        view.SetValue(AppearingProperty, value);

    public static ICommand? GetDisappearing(BindableObject view) =>
        (ICommand?)view.GetValue(DisappearingProperty);

    public static void SetDisappearing(BindableObject view, ICommand? value) =>
        view.SetValue(DisappearingProperty, value);

    private static void OnAppearingChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not Page page)
            return;
        page.Appearing -= OnAppearing;
        if (newValue is ICommand)
            page.Appearing += OnAppearing;
    }

    private static void OnDisappearingChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not Page page)
            return;
        page.Disappearing -= OnDisappearing;
        if (newValue is ICommand)
            page.Disappearing += OnDisappearing;
    }

    private static void OnAppearing(object? sender, EventArgs args) =>
        Execute(GetAppearing((Page)sender!));

    private static void OnDisappearing(object? sender, EventArgs args) =>
        Execute(GetDisappearing((Page)sender!));

    private static void Execute(ICommand? command)
    {
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }
}
