using System.Globalization;

namespace _4Life.Converters
{
    // Returneaza inversul unui bool
    // Folosit in ForgotPasswordPage pentru a dezactiva campul de email dupa verificare
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;
    }
}
