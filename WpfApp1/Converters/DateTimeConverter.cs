using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    /// <summary>
    /// Конвертер для форматирования даты и времени
    /// </summary>
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                string format = parameter as string ?? "dd.MM.yyyy HH:mm:ss";
                return dateTime.ToString(format);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string dateString)
            {
                string format = parameter as string ?? "dd.MM.yyyy HH:mm:ss";
                if (DateTime.TryParseExact(dateString, format, culture, DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }
            return DateTime.Now;
        }
    }
    
    /// <summary>
    /// Конвертер для отображения относительного времени (например, "5 минут назад")
    /// </summary>
    public class RelativeDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var now = DateTime.Now;
                var diff = now - dateTime;
                
                if (diff.TotalSeconds < 60)
                    return "только что";
                
                if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes} {GetMinutesForm((int)diff.TotalMinutes)} назад";
                
                if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours} {GetHoursForm((int)diff.TotalHours)} назад";
                
                if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays} {GetDaysForm((int)diff.TotalDays)} назад";
                
                return dateTime.ToString("dd.MM.yyyy HH:mm");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        private string GetMinutesForm(int minutes)
        {
            if (minutes % 10 == 1 && minutes != 11)
                return "минуту";
            
            if ((minutes % 10 == 2 || minutes % 10 == 3 || minutes % 10 == 4) && 
                !(minutes >= 12 && minutes <= 14))
                return "минуты";
            
            return "минут";
        }
        
        private string GetHoursForm(int hours)
        {
            if (hours % 10 == 1 && hours != 11)
                return "час";
            
            if ((hours % 10 == 2 || hours % 10 == 3 || hours % 10 == 4) && 
                !(hours >= 12 && hours <= 14))
                return "часа";
            
            return "часов";
        }
        
        private string GetDaysForm(int days)
        {
            if (days % 10 == 1 && days != 11)
                return "день";
            
            if ((days % 10 == 2 || days % 10 == 3 || days % 10 == 4) && 
                !(days >= 12 && days <= 14))
                return "дня";
            
            return "дней";
        }
    }
}
