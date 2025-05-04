using System;
using System.Globalization;
using System.Windows.Data;
using WpfApp1.Models;

namespace WpfApp1.Converters
{
    /// <summary>
    /// Конвертер для отображения названия роли пользователя
    /// </summary>
    public class UserRoleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is UserRole role) {
                return role.GetRoleName();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Конвертер для отображения описания роли пользователя
    /// </summary>
    public class UserRoleDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserRole role)
            {
                return role.GetRoleDescription();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Конвертер для определения видимости элементов управления в зависимости от роли пользователя
    /// </summary>
    public class RoleToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is UserRole role && parameter is string requiredRoles) {
                var roles = requiredRoles.Split(',');
            
                foreach (var requiredRole in roles) {
                    if (Enum.TryParse<UserRole>(requiredRole.Trim(), out var parsedRole) && role == parsedRole) {
                        return System.Windows.Visibility.Visible;
                    }
                }
                return System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
