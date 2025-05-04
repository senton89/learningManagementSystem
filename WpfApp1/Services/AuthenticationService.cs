using System;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class AuthenticationService
    {
        private readonly LmsDbContext _context;
        private readonly AuditService _auditService;
        private const int MaxFailedAttempts = 5;
        private const int LockoutDurationMinutes = 30;
        private const int MinPasswordLength = 8;

        public AuthenticationService(LmsDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        /// <summary>
        /// Аутентификация пользователя
        /// </summary>
        public async Task<(bool success, string message, User user)> AuthenticateAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Credentials)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    await _auditService.LogActionAsync("System", "AuthenticationFailed",
                        $"Попытка входа с несуществующим пользователем: {username}");
                    return (false, "Неверное имя пользователя или пароль", null);
                }

                if (!user.IsActive)
                {
                    await _auditService.LogActionAsync(user.Id.ToString(), "AuthenticationFailed",
                        "Попытка входа в неактивный аккаунт");
                    return (false, "Аккаунт деактивирован", null);
                }

                if (user.Credentials.LockoutEndDate.HasValue && user.Credentials.LockoutEndDate > DateTime.UtcNow)
                {
                    await _auditService.LogActionAsync(user.Id.ToString(), "AuthenticationFailed",
                        "Попытка входа в заблокированный аккаунт");
                    return (false, $"Аккаунт заблокирован до {user.Credentials.LockoutEndDate.Value:g}", null);
                }

                if (!VerifyPassword(password, user.Credentials.PasswordHash, user.Credentials.PasswordSalt))
                {
                    user.Credentials.FailedLoginAttempts++;
                    user.Credentials.LastFailedLoginDate = DateTime.UtcNow;

                    if (user.Credentials.FailedLoginAttempts >= MaxFailedAttempts)
                    {
                        user.Credentials.LockoutEndDate = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                        await _auditService.LogActionAsync(user.Id.ToString(), "AccountLocked",
                            $"Аккаунт заблокирован на {LockoutDurationMinutes} минут после {MaxFailedAttempts} неудачных попыток входа");
                    }

                    await _context.SaveChangesAsync();
                    await _auditService.LogActionAsync(user.Id.ToString(), "AuthenticationFailed",
                        $"Неудачная попытка входа. Попытка {user.Credentials.FailedLoginAttempts} из {MaxFailedAttempts}");

                    return (false, "Неверное имя пользователя или пароль", null);
                }

                // Сброс счетчика неудачных попыток при успешном входе
                user.Credentials.FailedLoginAttempts = 0;
                user.Credentials.LastFailedLoginDate = null;
                user.LastLoginDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(user.Id.ToString(), "AuthenticationSuccess",
                    "Успешный вход в систему");

                return (true, user.Credentials.RequirePasswordChange ? "Требуется смена пароля" : "Успешный вход",
                    user);
            }
            catch (Exception ex)
            {
                await _auditService.LogActionAsync("System", "AuthenticationError",
                    $"Ошибка при аутентификации: {ex.Message}");
                return (false, "Произошла ошибка при аутентификации", null);
            }
        }

        /// <summary>
        /// Создание хэша пароля
        /// </summary>
        public (string hash, string salt) HashPassword(string password)
        {
            using var rng = new RNGCryptoServiceProvider();
            var saltBytes = new byte[16];
            rng.GetBytes(saltBytes);
            var salt = Convert.ToBase64String(saltBytes);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000);
            var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));

            return (hash, salt);
        }

        /// <summary>
        /// Проверка пароля
        /// </summary>
        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            try
            {
                var saltBytes = Convert.FromBase64String(storedSalt);
                using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000);
                var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
                return hash == storedHash;
            }
            catch 
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
        }

        /// <summary>
        /// Изменение пароля пользователя
        /// </summary>
        public async Task<(bool success, string message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users
                .Include(u => u.Credentials)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return (false, "Пользователь не найден");

            if (!VerifyPassword(currentPassword, user.Credentials.PasswordHash, user.Credentials.PasswordSalt))
            {
                await _auditService.LogActionAsync(user.Id.ToString(), "PasswordChangeFailed", 
                    "Неудачная попытка смены пароля: неверный текущий пароль");
                return (false, "Неверный текущий пароль");
            }

            if (!ValidatePassword(newPassword, out string validationMessage))
            {
                await _auditService.LogActionAsync(user.Id.ToString(), "PasswordChangeFailed", 
                    $"Неудачная попытка смены пароля: {validationMessage}");
                return (false, validationMessage);
            }

            var (hash, salt) = HashPassword(newPassword);
            user.Credentials.PasswordHash = hash;
            user.Credentials.PasswordSalt = salt;
            user.Credentials.PasswordChangedDate = DateTime.UtcNow;
            user.Credentials.RequirePasswordChange = false;

            await _context.SaveChangesAsync();
            await _auditService.LogActionAsync(user.Id.ToString(), "PasswordChanged", 
                "Пароль успешно изменен");

            return (true, "Пароль успешно изменен");
        }

        /// <summary>
        /// Валидация пароля
        /// </summary>
        private bool ValidatePassword(string password, out string message)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                message = "Пароль не может быть пустым";
                return false;
            }

            if (password.Length < MinPasswordLength)
            {
                message = $"Пароль должен содержать не менее {MinPasswordLength} символов";
                return false;
            }

            if (!password.Any(char.IsUpper))
            {
                message = "Пароль должен содержать хотя бы одну заглавную букву";
                return false;
            }

            if (!password.Any(char.IsLower))
            {
                message = "Пароль должен содержать хотя бы одну строчную букву";
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                message = "Пароль должен содержать хотя бы одну цифру";
                return false;
            }

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                message = "Пароль должен содержать хотя бы один специальный символ";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// Создание токена для сброса пароля
        /// </summary>
        public async Task<string> GeneratePasswordResetTokenAsync(string username)
        {
            var user = await _context.Users
                .Include(u => u.Credentials)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return null;

            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.Credentials.PasswordResetToken = token;
            user.Credentials.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);

            await _context.SaveChangesAsync();
            await _auditService.LogActionAsync(user.Id.ToString(), "PasswordResetTokenGenerated", 
                "Создан токен для сброса пароля");

            return token;
        }

        /// <summary>
        /// Сброс пароля с использованием токена
        /// </summary>
        public async Task<(bool success, string message)> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Users
                .Include(u => u.Credentials)
                .FirstOrDefaultAsync(u => u.Credentials.PasswordResetToken == token);

            if (user == null)
                return (false, "Недействительный токен");

            if (user.Credentials.PasswordResetTokenExpiry < DateTime.UtcNow)
                return (false, "Срок действия токена истек");

            if (!ValidatePassword(newPassword, out string validationMessage))
                return (false, validationMessage);

            var (hash, salt) = HashPassword(newPassword);
            user.Credentials.PasswordHash = hash;
            user.Credentials.PasswordSalt = salt;
            user.Credentials.PasswordChangedDate = DateTime.UtcNow;
            user.Credentials.PasswordResetToken = null;
            user.Credentials.PasswordResetTokenExpiry = null;
            user.Credentials.RequirePasswordChange = false;

            await _context.SaveChangesAsync();
            await _auditService.LogActionAsync(user.Id.ToString(), "PasswordReset", 
                "Пароль успешно сброшен");

            return (true, "Пароль успешно сброшен");
        }
    }
}
