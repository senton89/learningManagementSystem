using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class ProgressToAngleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return 0.0;

            if (values[0] is double value && values[1] is double maximum && maximum != 0)
            {
                // Calculate angle as percentage of maximum (0 to 360 degrees)
                double percentage = value / maximum;
                return percentage * 360.0;
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
