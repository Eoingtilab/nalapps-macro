using System.Globalization;
using System.Windows.Data;

namespace NalApps.Macro.Converters;

public sealed class MillisecondsToSecondsConverter : IValueConverter
{
    private const double MaxSeconds = 86400d;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!int.TryParse(value?.ToString(), out var milliseconds)) return "1";
        var seconds = Math.Clamp(milliseconds / 1000d, 0d, MaxSeconds);
        return seconds.ToString("0.###", CultureInfo.CurrentCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return Binding.DoNothing;

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var seconds) &&
            !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
        {
            return Binding.DoNothing;
        }

        seconds = Math.Clamp(seconds, 0d, MaxSeconds);
        return ((int)Math.Round(seconds * 1000d)).ToString(CultureInfo.InvariantCulture);
    }
}
