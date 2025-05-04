using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class BoolToRequiredConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isRequired = (bool)value;
            string fieldName = parameter as string ?? "Field";
            
            return isRequired ? $"{fieldName} *" : fieldName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
