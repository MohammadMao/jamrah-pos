using System.Globalization;
using System.Windows.Data;

namespace JamrahPOS.Converters
{
    /// <summary>
    /// Converts decimal to formatted currency string
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return $"{decimalValue:N2} SDG";
            }
            return "0.00 SDG";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
