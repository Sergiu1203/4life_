using System.Globalization;

namespace _4Life.Converters
{
    // Returneaza o culoare de fundal diferita pentru medicamentele adaugate de pacient
    // true (patient added) → fundal lavanda subtil
    // false (prescribed)   → fundal alb / transparent
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Color.FromArgb("#F3F0FF"); // lavanda foarte deschis
            return Colors.White;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
