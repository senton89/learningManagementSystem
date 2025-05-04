using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class AuditService
    {
        private readonly LmsDbContext _context;
        private readonly IDbContextFactory<LmsDbContext> _contextFactory;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public AuditService(LmsDbContext context)
        {
            _context = context;
            _contextFactory = null;
        }
        public AuditService(IDbContextFactory<LmsDbContext> contextFactory)
        {
            _context = null;
            _contextFactory = contextFactory;
        }

        public async Task LogActionAsync(string userId, string action, string details)
        {
            try
            {
                await _semaphore.WaitAsync();
            
                if (_contextFactory != null)
                {
                    using var context = await _contextFactory.CreateDbContextAsync();
                    var logEntry = new AuditLog
                    {
                        UserId = userId,
                        Action = action,
                        Details = details,
                        Timestamp = DateTime.UtcNow,
                    };
            
                    await context.AuditLogs.AddAsync(logEntry);
                    await context.SaveChangesAsync();
                }
                else if (_context != null)
                {
                    var logEntry = new AuditLog
                    {
                        UserId = userId,
                        Action = action,
                        Details = details,
                        Timestamp = DateTime.UtcNow,
                    };
            
                    await _context.AuditLogs.AddAsync(logEntry);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем MessageBox, так как это может вызвать циклические ошибки
                Console.WriteLine($"Error while logging: {ex}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Логирует изменение роли пользователя
        /// </summary>
        public async Task LogRoleChangeAsync(User user, UserRole oldRole, UserRole newRole, string changedByUserId)
        {
            await LogActionAsync(
                user.Id.ToString(),
                "RoleChanged",
                $"Роль пользователя {user.Username} изменена с {oldRole.GetRoleName()} на {newRole.GetRoleName()} пользователем {changedByUserId}"
            );
        }
        
        /// <summary>
        /// Логирует изменение разрешений пользователя
        /// </summary>
        public async Task LogPermissionChangeAsync(User user, Permission permission, bool isGranted, string changedByUserId)
        {
            var action = isGranted ? "PermissionGranted" : "PermissionRevoked";
            var actionText = isGranted ? "выдано" : "отозвано";
            
            await LogActionAsync(
                user.Id.ToString(),
                action,
                $"Разрешение '{permission.Name}' {actionText} для пользователя {user.Username} пользователем {changedByUserId}"
            );
        }
        
        /// <summary>
        /// Получает историю изменений ролей для указанного пользователя
        /// </summary>
        public async Task<List<AuditLog>> GetUserRoleHistoryAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AuditLogs
                .Where(log => log.UserId == userId.ToString() && log.Action == "RoleChanged")
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }
        
        /// <summary>
        /// Получает историю изменений разрешений для указанного пользователя
        /// </summary>
        public async Task<List<AuditLog>> GetUserPermissionHistoryAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AuditLogs
                .Where(log => log.UserId == userId.ToString() && 
                       (log.Action == "PermissionGranted" || log.Action == "PermissionRevoked"))
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }
        
        /// <summary>
        /// Получает все действия, выполненные указанным пользователем
        /// </summary>
        public async Task<List<AuditLog>> GetUserActionsAsync(int performedByUserId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AuditLogs
                .Where(log => log.Details.Contains($"пользователем {performedByUserId}"))
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }
    }
}
