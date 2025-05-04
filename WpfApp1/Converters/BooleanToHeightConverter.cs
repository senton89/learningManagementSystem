using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class BooleanToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return 100; // Высота для многострочного текста
            }
            return 50; // Высота для однострочного текста
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}