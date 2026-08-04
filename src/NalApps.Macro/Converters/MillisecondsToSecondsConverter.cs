using System.Globalization;
using System.Windows.Data;

namespace NalApps.Macro.Converters;

public sealed class MillisecondsToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!int.TryParse(value?.ToString(), out var milliseconds)) return "1";
        return (milliseconds / 1000d).ToString("0.###", CultureInfo.InvariantCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString()?.Trim();
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            return Binding.DoNothing;

        seconds = Math.Max(0, seconds);
        return ((int)Math.Round(seconds * 1000d)).ToString(CultureInfo.InvariantCulture);
    }
}
