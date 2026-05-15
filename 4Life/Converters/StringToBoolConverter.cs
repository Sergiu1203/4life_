using System.Globalization;

namespace _4Life.Converters
{
    // Returneaza true daca string-ul nu e gol
    // Folosit pentru a arata/ascunde mesaje de eroare
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !string.IsNullOrEmpty(value as string);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
