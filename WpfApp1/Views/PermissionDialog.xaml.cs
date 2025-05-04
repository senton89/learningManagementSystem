using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WpfApp1.Models;

namespace WpfApp1.Views
{
    public partial class PermissionDialog : Window
    {
        public Permission Permission { get; private set; }
        public ObservableCollection<RoleSelectionItem> AllRoles { get; set; }

        public PermissionDialog()
        {
            InitializeComponent();
            Permission = new Permission();
            InitializeDialog();
        }

        public PermissionDialog(Permission permission) {
            InitializeComponent();
            Permission = new Permission {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Type = permission.Type,
                ResourceType = permission.ResourceType,
                ApplicableRoles = new List<PermissionRole>()
            };
        
            // Копируем существующие роли
            foreach (var role in permission.ApplicableRoles) {
                Permission.ApplicableRoles.Add(new PermissionRole {
                    Permission = Permission,
                    Role = role.Role
                });
            }
        
            InitializeDialog();
        }


        private void InitializeDialog() {
            // Инициализируем список ролей
            AllRoles = new ObservableCollection<RoleSelectionItem>();
            foreach (var role in UserRoleExtensions.AllRoles()) {
                AllRoles.Add(new RoleSelectionItem {
                    Role = role,
                    IsSelected = Permission.ApplicableRoles?.Any(pr => pr.Role == role.ToString()) ?? false
                });
            }

            DataContext = Permission;
            Resources.Add("UserRoleConverter", new UserRoleConverter());
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(Permission.Name))
            {
                MessageBox.Show("Название разрешения не может быть пустым", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохраняем выбранные роли в объект Permission
            Permission.ApplicableRoles.Clear();
            foreach (var roleItem in AllRoles.Where(r => r.IsSelected))
            {
                Permission.ApplicableRoles.Add(new PermissionRole 
                { 
                    Permission = Permission,
                    Role = roleItem.Role.ToString() 
                });
            }

            DialogResult = true;
            Close();
        }
    }

    public class RoleSelectionItem
    {
        public UserRole Role { get; set; }
        public bool IsSelected { get; set; }
    }

    public class UserRoleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is UserRole role)
            {
                return role.GetRoleName();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
