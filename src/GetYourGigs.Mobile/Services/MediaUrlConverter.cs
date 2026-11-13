using System.Globalization;

namespace GetYourGigs.Mobile.Services;

public sealed class MediaUrlConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;
        return url.StartsWith("/uploads/", StringComparison.Ordinal)
            ? new Uri(
                new Uri(Preferences.Default.Get("api-address", "http://localhost:5080/")),
                url
            ).ToString()
            : url;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
