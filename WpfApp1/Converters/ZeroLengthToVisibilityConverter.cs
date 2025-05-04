using System;
using System.Globalization;
using System.Security;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class ZeroLengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int length)
            {
                return length == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class PasswordToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int password)
            {
                bool isEmpty = password == 0;
                bool invert = parameter != null && parameter.ToString().ToLower() == "invert";

                if (invert)
                    isEmpty = !isEmpty;

                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
