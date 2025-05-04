using System;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    /// <summary>
    /// Модель для хранения учетных данных пользователя
    /// </summary>
    public class UserCredentials
    {
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        [Key]
        public int UserId { get; set; }
        
        /// <summary>
        /// Хэш пароля
        /// </summary>
        [Required]
        public string PasswordHash { get; set; }
        
        /// <summary>
        /// Соль для пароля
        /// </summary>
        [Required]
        public string PasswordSalt { get; set; }
        
        /// <summary>
        /// Дата последнего изменения пароля
        /// </summary>
        public DateTime PasswordChangedDate { get; set; }
        
        /// <summary>
        /// Требуется ли смена пароля при следующем входе
        /// </summary>
        public bool RequirePasswordChange { get; set; }
        
        /// <summary>
        /// Количество неудачных попыток входа
        /// </summary>
        public int FailedLoginAttempts { get; set; }
        
        /// <summary>
        /// Дата последней неудачной попытки входа
        /// </summary>
        public DateTime? LastFailedLoginDate { get; set; }
        
        /// <summary>
        /// Дата блокировки аккаунта
        /// </summary>
        public DateTime? LockoutEndDate { get; set; }
        
        /// <summary>
        /// Секретный ключ для двухфакторной аутентификации
        /// </summary>
        public string TwoFactorSecretKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Включена ли двухфакторная аутентификация
        /// </summary>
        public bool IsTwoFactorEnabled { get; set; }
        
        /// <summary>
        /// Токен для восстановления пароля
        /// </summary>
        public string PasswordResetToken { get; set; }
        
        /// <summary>
        /// Дата истечения токена для восстановления пароля
        /// </summary>
        public DateTime? PasswordResetTokenExpiry { get; set; }
        
        /// <summary>
        /// Связь с пользователем
        /// </summary>
        public virtual User User { get; set; }
    }
}
