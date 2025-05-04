using System;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    /// <summary>
    /// Модель для хранения информации о действиях пользователей в системе
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// Уникальный идентификатор записи
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Тип действия (например, "RoleChanged", "PermissionGranted", "Login")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Action { get; set; }
        
        /// <summary>
        /// Подробное описание действия
        /// </summary>
        [MaxLength(1000)]
        public string Details { get; set; }
        
        /// <summary>
        /// Дата и время выполнения действия
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Идентификатор пользователя, над которым выполнено действие
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string UserId { get; set; }
        
        /// <summary>
        /// Идентификатор пользователя, выполнившего действие (если применимо)
        /// </summary>
        [MaxLength(50)]
        public string? PerformedByUserId { get; set; }
        
        /// <summary>
        /// IP-адрес, с которого выполнено действие (если применимо)
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// Результат действия (успех, ошибка и т.д.)
        /// </summary>
        [MaxLength(50)]
        public string? Result { get; set; }
        
        /// <summary>
        /// Категория действия (например, "Security", "UserManagement", "CourseManagement")
        /// </summary>
        [MaxLength(50)]
        public string? Category { get; set; }
    }
}
