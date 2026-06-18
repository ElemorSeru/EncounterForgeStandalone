using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EncounterForgeStandalone.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is Visibility.Visible;
}

[ValueConversion(typeof(bool), typeof(bool))]
class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is not true;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => value is not true;
}

[ValueConversion(typeof(string), typeof(Brush))]
class OutcomeColorConverter : IValueConverter
{
    static readonly SolidColorBrush Easy = new(Color.FromRgb(0x4A, 0x9F, 0x5A));
    static readonly SolidColorBrush Manageable = new(Color.FromRgb(0x7B, 0xAF, 0x4A));
    static readonly SolidColorBrush Risky = new(Color.FromRgb(0xD4, 0x97, 0x3A));
    static readonly SolidColorBrush Dangerous = new(Color.FromRgb(0xC0, 0x40, 0x40));
    static readonly SolidColorBrush Fallback = new(Color.FromRgb(0x3A, 0x3A, 0x48));

    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch
    {
        "easy" => Easy,
        "manageable" => Manageable,
        "risky" => Risky,
        "dangerous" => Dangerous,
        _ => Fallback
    };
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

[ValueConversion(typeof(double), typeof(double))]
class RoundsToWidthConverter : IMultiValueConverter
{
    const double MaxRounds = 9.0;

    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        if (values[0] is not double rounds || values[1] is not double totalWidth) return 0.0;
        return Math.Min(rounds / MaxRounds, 1.0) * totalWidth;
    }
    public object[] ConvertBack(object value, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
}
