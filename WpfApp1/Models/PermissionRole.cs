namespace WpfApp1.Models;

public class PermissionRole
{
    public int Id { get; set; }
    public int PermissionId { get; set; }
    public string Role { get; set; } // Будем хранить роль как строку
    
    public virtual Permission Permission { get; set; }
}