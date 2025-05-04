using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WpfApp1.Models;

namespace WpfApp1.Converters
{

    // public class BooleanToVisibilityConverter : IValueConverter
    // {
    //     public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //     {
    //         return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
    //     }
    //
    //     public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //     {
    //         return value is Visibility && (Visibility)value == Visibility.Visible;
    //     }
    // }

    // public class InverseBooleanToVisibilityConverter : IValueConverter
    // {
    //     public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //     {
    //         return (value is bool && (bool)value) ? Visibility.Collapsed : Visibility.Visible;
    //     }
    //
    //     public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //     {
    //         return value is Visibility && (Visibility)value == Visibility.Collapsed;
    //     }
    // }

    public class SubmissionStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SubmissionStatus status)
            {
                return status switch
                {
                    SubmissionStatus.Draft => Brushes.Gray,
                    SubmissionStatus.Submitted => Brushes.Orange,
                    SubmissionStatus.UnderReview => Brushes.Blue,
                    SubmissionStatus.Reviewed => Brushes.Green,
                    SubmissionStatus.RequiresRevision => Brushes.Red,
                    _ => Brushes.Black
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SubmissionStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SubmissionStatus status)
            {
                return status switch
                {
                    SubmissionStatus.Draft => "Черновик",
                    SubmissionStatus.Submitted => "На проверке",
                    SubmissionStatus.UnderReview => "Проверяется",
                    SubmissionStatus.Reviewed => "Проверено",
                    SubmissionStatus.RequiresRevision => "Требуется доработка",
                    _ => string.Empty
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // public class FileSizeConverter : IValueConverter
    // {
    //     public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //     {
    //         if (value is long size)
    //         {
    //             if (size < 1024)
    //                 return $"{size} Б";
    //             if (size < 1024 * 1024)
    //                 return $"{size / 1024:F1} КБ";
    //             return $"{size / (1024 * 1024):F1} МБ";
    //         }
    //         return "0 Б";
    //     }
    //
    //     public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }

    public class DateTimeToRemainingTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dueDate)
            {
                var timeRemaining = dueDate - DateTime.UtcNow;

                if (timeRemaining.TotalDays > 1)
                    return $"Осталось {(int)timeRemaining.TotalDays} дней";
                if (timeRemaining.TotalHours > 1)
                    return $"Осталось {(int)timeRemaining.TotalHours} часов";
                if (timeRemaining.TotalMinutes > 0)
                    return $"Осталось {(int)timeRemaining.TotalMinutes} минут";
                return "Срок сдачи истек";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeToRemainingBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dueDate)
            {
                var timeRemaining = dueDate - DateTime.UtcNow;

                if (timeRemaining.TotalDays > 1)
                    return Brushes.Black;
                if (timeRemaining.TotalHours > 1)
                    return Brushes.Orange;
                return Brushes.Red;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
