using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public DateTime RegistrationDate { get; set; }
    public virtual ICollection<Course>? EnrolledCourses { get; set; }
    public virtual ICollection<Course>? TeachingCourses { get; set; }
    
    // Дополнительные пользовательские разрешения
    public virtual ICollection<Permission>? CustomPermissions { get; set; }
    public virtual ICollection<UserPermission>? Permissions { get; set; } 
    public virtual UserCredentials? Credentials { get; set; } // Удалено дублирующее свойство Credentials
    
    // Полное имя пользователя
    public string FullName => $"{FirstName} {LastName}";
    
    // Последний вход в систему
    public DateTime? LastLoginDate { get; set; }
    
    // Статус активности
    public bool IsActive { get; set; } = true;
}

public enum UserRole
{
    Student,
    Teacher,
    Administrator
}

public static class UserExtensions
{
    public static bool IsTeacher(this User user)
    {
        return user.Role == UserRole.Teacher;
    }
    
    public static bool IsAdmin(this User user)
    {
        return user.Role == UserRole.Administrator;
    }
    
    public static bool HasRole(this User user, UserRole role)
    {
        return user.Role == role;
    }
}