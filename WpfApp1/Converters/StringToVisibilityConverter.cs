using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    /// <summary>
    /// Конвертер для преобразования строки в видимость (пустая строка - Collapsed, непустая - Visible)
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                bool invert = parameter is string paramStr && paramStr.ToLower() == "invert";
                bool isEmpty = string.IsNullOrWhiteSpace(str);
                
                if (invert)
                    return isEmpty ? Visibility.Visible : Visibility.Collapsed;
                else
                    return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Конвертер для преобразования булевого значения в видимость
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool invert = parameter is string paramStr && paramStr.ToLower() == "invert";
                
                if (invert)
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                else
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool invert = parameter is string paramStr && paramStr.ToLower() == "invert";
                bool isVisible = visibility == Visibility.Visible;
                
                if (invert)
                    return !isVisible;
                else
                    return isVisible;
            }
            
            return false;
        }
    }
}
