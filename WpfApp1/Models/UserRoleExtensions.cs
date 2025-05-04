using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1.Models
{
    /// <summary>
    /// Расширения для перечисления UserRole
    /// </summary>
    public static class UserRoleExtensions
    {
        /// <summary>
        /// Возвращает название роли в удобочитаемом формате
        /// </summary>
        public static string GetRoleName(this UserRole role)
        {
            return role switch
            {
                UserRole.Student => "Студент",
                UserRole.Teacher => "Преподаватель",
                UserRole.Administrator => "Администратор",
                _ => "Неизвестная роль"
            };
        }
        
        /// <summary>
        /// Returns all available user roles
        /// </summary>
        public static IEnumerable<UserRole> AllRoles()
        {
            return Enum.GetValues(typeof(UserRole))
                .Cast<UserRole>()
                .ToList();
        }

        /// <summary>
        /// Возвращает описание роли
        /// </summary>
        public static string GetRoleDescription(this UserRole role)
        {
            return role switch
            {
                UserRole.Student => "Доступ к курсам и учебным материалам",
                UserRole.Teacher => "Управление курсами и контентом, мониторинг прогресса студентов",
                UserRole.Administrator => "Полный доступ к системе, включая управление пользователями и ролями",
                _ => "Неизвестная роль"
            };
        }

        /// <summary>
        /// Возвращает список базовых разрешений для роли
        /// </summary>
        public static List<string> GetDefaultPermissions(this UserRole role)
        {
            return role switch
            {
                UserRole.Student => new List<string> { "ViewCourses" },
                UserRole.Teacher => new List<string> { "ViewCourses", "CreateCourse", "EditCourse", "ViewUsers" },
                UserRole.Administrator => new List<string> { 
                    "ViewCourses", "CreateCourse", "EditCourse", "DeleteCourse", 
                    "ViewUsers", "CreateUser", "EditUser", "DeleteUser", 
                    "AssignRole", "ViewPermissions", "ManagePermissions" 
                },
                _ => new List<string>()
            };
        }
        
        /// <summary>
        /// Проверяет, может ли пользователь с указанной ролью изменить роль другого пользователя
        /// </summary>
        public static bool CanChangeRole(this UserRole currentUserRole, UserRole targetUserRole, UserRole newRole)
        {
            // Администратор может изменить роль любого пользователя
            if (currentUserRole == UserRole.Administrator)
                return true;
    
            // Преподаватель может изменить роль только студента и только на роль студента или преподавателя
            if (currentUserRole == UserRole.Teacher)
                return targetUserRole == UserRole.Student &&
                       (newRole == UserRole.Student || newRole == UserRole.Teacher);
    
            // Студенты не могут изменять роли
            return false;
        }
    }
}
