using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.Infrastructure;

public static class DbInitializer
{
    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
        
        // Apply migrations
        await dbContext.Database.MigrateAsync();
        
        // Seed admin user if none exists
        if (!await dbContext.Users.AnyAsync(u => u.Role == UserRole.Administrator))
        {
            var (hash, salt) = authService.HashPassword("Admin123!");
            
            var admin = new User
            {
                Username = "admin",
                FirstName = "System",
                LastName = "Administrator",
                Email = "admin@example.com",
                Role = UserRole.Administrator,
                RegistrationDate = DateTime.UtcNow,
                IsActive = true
            };
            
            var credentials = new UserCredentials 
            {
                PasswordHash = hash,
                PasswordSalt = salt,
                PasswordChangedDate = DateTime.UtcNow,
                RequirePasswordChange = false,
                TwoFactorSecretKey = string.Empty, // Добавляем пустую строку вместо null
                IsTwoFactorEnabled = false,
                PasswordResetToken = salt + "-" + hash
            };
            
            admin.Credentials = credentials;
            
            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync();
        }
        
        // Seed basic permissions if none exist
        if (!await dbContext.Permissions.AnyAsync())
        {
            var permissions = new List<Permission>
            {
                new Permission { Name = "ViewCourses", Description = "View available courses", Type = PermissionType.Read, ResourceType = "Course" },
                new Permission { Name = "CreateCourse", Description = "Create new courses", Type = PermissionType.Create, ResourceType = "Course" },
                new Permission { Name = "EditCourse", Description = "Edit existing courses", Type = PermissionType.Update, ResourceType = "Course" },
                new Permission { Name = "DeleteCourse", Description = "Delete courses", Type = PermissionType.Delete, ResourceType = "Course" },
                new Permission { Name = "ViewUsers", Description = "View user list", Type = PermissionType.Read, ResourceType = "User" },
                new Permission { Name = "CreateUser", Description = "Create new users", Type = PermissionType.Create, ResourceType = "User" },
                new Permission { Name = "EditUser", Description = "Edit user profiles", Type = PermissionType.Update, ResourceType = "User" },
                new Permission { Name = "DeleteUser", Description = "Delete users", Type = PermissionType.Delete, ResourceType = "User" },
                new Permission { Name = "AssignRole", Description = "Assign roles to users", Type = PermissionType.Assign, ResourceType = "User" },
                new Permission { Name = "ManagePermissions", Description = "Manage user permissions", Type = PermissionType.Update, ResourceType = "Permission" }
            };
            
            foreach (var permission in permissions)
            {
                // Add applicable roles
                var roles = new List<PermissionRole>();
                
                if (permission.Name == "ViewCourses")
                {
                    roles.Add(new PermissionRole { Permission = permission, Role = UserRole.Student.ToString() });
                    roles.Add(new PermissionRole { Permission = permission, Role = UserRole.Teacher.ToString() });
                    roles.Add(new PermissionRole { Permission = permission, Role = UserRole.Administrator.ToString() });
                }
                else if (permission.Name == "CreateCourse" || permission.Name == "EditCourse")
                {
                    roles.Add(new PermissionRole { Permission = permission, Role = UserRole.Teacher.ToString() });
                    roles.Add(new PermissionRole { Permission = permission, Role = UserRole.Administrator.ToString() });
                }
                else
                {
                    roles.Add(new PermissionRole { Permission = permission, Role = UserRole.Administrator.ToString() });
                }
                
                permission.ApplicableRoles = roles;
                dbContext.Permissions.Add(permission);
            }
            
            await dbContext.SaveChangesAsync();
        }
    }
}