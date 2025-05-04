using System.Collections.Generic;

namespace WpfApp1.Models
{
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }
        
        // Связь с ролями пользователей
        // public virtual ICollection<UserRole> ApplicableRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<PermissionRole> ApplicableRoles { get; set; } = new List<PermissionRole>();
        
        public virtual ICollection<UserPermission>? Users { get; set; } // Изменено на UserPermission
        
        // Операции, которые разрешение позволяет делать
        public PermissionType Type { get; set; }
        
        // Ресурс, к которому применяется разрешение
        public string ResourceType { get; set; }
    }
    
    public enum PermissionType
    {
        Read,
        Create,
        Update,
        Delete,
        Approve,
        Assign
    }
}
