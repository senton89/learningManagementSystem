using System;

namespace WpfApp1.Models
{
    public class UserPermission
    {
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        
        public virtual User User { get; set; }
        public virtual Permission Permission { get; set; }
        
        // Специальные флаги для этого разрешения у данного пользователя
        public bool IsGranted { get; set; } = true;
        
        // Дата выдачи разрешения
        public DateTime GrantedDate { get; set; } = DateTime.Now;
        
        // Кто выдал разрешение
        public int? GrantedByUserId { get; set; }
    }
}
