using System.Globalization;

namespace _4Life.Converters
{
    // "Ion Popescu" -> "IP", "Maria" -> "M"
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string name || string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : parts[0][0].ToString().ToUpper();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
