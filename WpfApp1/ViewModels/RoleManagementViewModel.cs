using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Views;

namespace WpfApp1.ViewModels;

public class RoleManagementViewModel : NotifyPropertyChangedBase
{
    public ObservableCollection<User> Users { get; set; }
    private ObservableCollection<Permission> _permissions;
    public ObservableCollection<Permission> Permissions
    {
        get => _permissions;
        set => SetProperty(ref _permissions, value);
    }

    private ObservableCollection<UserRole> _availableRoles;
    public ObservableCollection<UserRole> AvailableRoles
    {
        get => _availableRoles;
        set => SetProperty(ref _availableRoles, value);
    }

    public ICommand AddPermissionCommand { get; }
    public ICommand EditPermissionCommand { get; }
    public ICommand DeletePermissionCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AssignPermissionCommand { get; }
    public ICommand RevokePermissionCommand { get; }
    public ICommand FilterUsersCommand { get; }

    private readonly LmsDbContext _context;
    private readonly IDbContextFactory<LmsDbContext> _contextFactory;
    private readonly AuditService _auditService;
    private string _searchText;
    private User _selectedUser;
    private Permission _selectedPermission;
    private UserRole _selectedRoleFilter;

    public User SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value) && value != null)
            {
                LoadUserPermissions();
            }
        }
    }

    public Permission SelectedPermission
    {
        get => _selectedPermission;
        set => SetProperty(ref _selectedPermission, value);
    }

    public UserRole SelectedRoleFilter
    {
        get => _selectedRoleFilter;
        set
        {
            if (SetProperty(ref _selectedRoleFilter, value))
            {
                FilterUsers();
            }
        }
    }

    public RoleManagementViewModel(LmsDbContext context, AuditService auditService, IDbContextFactory<LmsDbContext> contextFactory)
    {
        _context = context;
        _contextFactory = contextFactory;
        _auditService = auditService;
        Users = new ObservableCollection<User>();
        Permissions = new ObservableCollection<Permission>();
        AvailableRoles = new ObservableCollection<UserRole>();
        foreach (var role in UserRoleExtensions.AllRoles())
        {
            AvailableRoles.Add(role);
        }
        LoadUsers();
        LoadPermissions();
        
        AddPermissionCommand = new RelayCommand(AddPermission);
        EditPermissionCommand = new RelayCommand(EditPermission);
        DeletePermissionCommand = new RelayCommand(DeletePermission);
        SearchCommand = new RelayCommand(Search);
        AssignPermissionCommand = new RelayCommand(AssignPermission);
        RevokePermissionCommand = new RelayCommand(RevokePermission);
        FilterUsersCommand = new RelayCommand(FilterUsers);
    }

    public async void LoadUsers()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.CustomPermissions)
                .ToListAsync();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync("System", "LoadUsersError", $"Error loading users: {ex.Message}");
            MessageBox.Show("Error loading users", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadPermissions() {
        
        using var context = await _contextFactory.CreateDbContextAsync();
        try {
            var permissions = await context.Permissions
                .Include(p => p.ApplicableRoles)
                .ToListAsync();
        
            Permissions.Clear();
            foreach (var permission in permissions) {
                Permissions.Add(permission);
            }
        } catch (Exception ex) {
            await _auditService.LogActionAsync("System", "LoadPermissionsError", $"Error loading permissions: {ex.Message}");
            MessageBox.Show("Error loading permissions", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadUserPermissions()
    {
        if (SelectedUser == null) return;

        try
        {
            // Загружаем все разрешения пользователя
            var userPermissions = await _context.UserPermissions
                .Where(up => up.UserId == SelectedUser.Id)
                .Include(up => up.Permission)
                .ToListAsync();

            // Обновляем флаг IsSelected в коллекции разрешений
            foreach (var permission in Permissions)
            {
                permission.IsSelected = userPermissions.Any(up => up.PermissionId == permission.Id);
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync(SelectedUser.Id.ToString(), "LoadUserPermissionsError", 
                $"Error loading permissions for user {SelectedUser.Username}: {ex.Message}");
            MessageBox.Show($"Error loading permissions for user {SelectedUser.Username}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async void UpdateUserRole(User user, UserRole newRole)
    {
        if (user == null) return;
        
        // Если роль не изменилась, не нужно ничего делать
        if (user.Role == newRole) return;
        
        try
        {
            var confirmMessage = $"Are you sure you want to change role for user {user.Username} from {user.Role.GetRoleName()} to {newRole.GetRoleName()}?";
            var result = MessageBox.Show(confirmMessage, "Confirm Role Change", 
                                      MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // Сохраняем старую роль для логирования
                var oldRole = user.Role;
                
                // Проверяем, не является ли данный пользователь последним администратором
                if (oldRole == UserRole.Administrator)
                {
                    var adminCount = Users.Count(u => u.Role == UserRole.Administrator);
                    if (adminCount <= 1 && newRole != UserRole.Administrator)
                    {
                        MessageBox.Show("Cannot change role: This is the last administrator user!", 
                                      "Security Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                
                user.Role = newRole;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                await _auditService.LogActionAsync(user.Id.ToString(), "RoleChanged", 
                                                $"User {user.Username} role changed from {oldRole} to {newRole}");
                
                MessageBox.Show($"User {user.Username} role changed to {newRole.GetRoleName()}", 
                              "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Если пользователь был выбран, обновляем его разрешения
                if (user == SelectedUser)
                {
                    LoadUserPermissions();
                }
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync(user.Id.ToString(), "RoleChangeError", 
                                            $"Error changing role for user {user.Username}: {ex.Message}");
            
            MessageBox.Show($"Error changing role for user {user.Username}", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddPermission()
    {
        try
        {
            // Создание диалога для ввода данных нового разрешения
            var permissionDialog = new PermissionDialog();
            
            if (permissionDialog.ShowDialog() == true)
            {
                var newPermission = permissionDialog.Permission;
                
                var confirmMessage = $"Are you sure you want to add permission {newPermission.Name}?";
                var result = MessageBox.Show(confirmMessage, "Confirm Add Permission", 
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    Permissions.Add(newPermission);
                    await _context.Permissions.AddAsync(newPermission);
                    await _context.SaveChangesAsync();
                    
                    await _auditService.LogActionAsync("System", "PermissionAdded", 
                                                    $"Permission {newPermission.Name} added");
                    
                    MessageBox.Show($"Permission {newPermission.Name} added", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync("System", "PermissionAddError", 
                                            $"Error adding permission: {ex.Message}");
            
            MessageBox.Show("Error adding permission", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void EditPermission()
    {
        try
        {
            if (SelectedPermission == null)
            {
                MessageBox.Show("Please select a permission to edit", 
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var permissionDialog = new PermissionDialog(SelectedPermission);
            
            if (permissionDialog.ShowDialog() == true)
            {
                var editedPermission = permissionDialog.Permission;
                
                var confirmMessage = $"Are you sure you want to edit permission {SelectedPermission.Name}?";
                var result = MessageBox.Show(confirmMessage, "Confirm Edit Permission", 
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Копируем изменения из диалога в выбранное разрешение
                    SelectedPermission.Name = editedPermission.Name;
                    SelectedPermission.Description = editedPermission.Description;
                    SelectedPermission.Type = editedPermission.Type;
                    SelectedPermission.ResourceType = editedPermission.ResourceType;
                    SelectedPermission.ApplicableRoles.Clear();
                    foreach (var role in editedPermission.ApplicableRoles)
                    {
                        SelectedPermission.ApplicableRoles.Add(new PermissionRole
                        {
                            Permission = SelectedPermission,
                            Role = role.Role
                        });
                    }
                    _context.Permissions.Update(SelectedPermission);
                    await _context.SaveChangesAsync();
                    
                    await _auditService.LogActionAsync("System", "PermissionEdited", 
                                                    $"Permission {SelectedPermission.Name} edited");
                    
                    MessageBox.Show("Permission edited successfully", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Обновляем отображение, если выбран пользователь
                    if (SelectedUser != null)
                    {
                        LoadUserPermissions();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync("System", "PermissionEditError", 
                                            $"Error editing permission: {ex.Message}");
            
            MessageBox.Show("Error editing permission", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeletePermission()
    {
        try
        {
            if (SelectedPermission == null)
            {
                MessageBox.Show("Please select a permission to delete", 
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var confirmMessage = $"Are you sure you want to delete permission {SelectedPermission.Name}?\nThis action cannot be undone and will remove this permission from all users!";
            var result = MessageBox.Show(confirmMessage, "Confirm Delete", 
                                      MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                // Проверка, используется ли разрешение
                var usageCount = await _context.UserPermissions
                    .Where(up => up.PermissionId == SelectedPermission.Id)
                    .CountAsync();
                
                if (usageCount > 0)
                {
                    var secondConfirm = MessageBox.Show(
                        $"This permission is currently used by {usageCount} users. Are you absolutely sure you want to delete it?",
                        "Warning - Permission in Use",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (secondConfirm != MessageBoxResult.Yes)
                    {
                        return;
                    }
                    
                    // Удаляем связи с пользователями
                    var userPermissions = await _context.UserPermissions
                        .Where(up => up.PermissionId == SelectedPermission.Id)
                        .ToListAsync();
                    
                    _context.UserPermissions.RemoveRange(userPermissions);
                }
                
                Permissions.Remove(SelectedPermission);
                _context.Permissions.Remove(SelectedPermission);
                await _context.SaveChangesAsync();
                
                await _auditService.LogActionAsync("System", "PermissionDeleted", 
                                                $"Permission {SelectedPermission.Name} deleted");
                
                MessageBox.Show($"Permission {SelectedPermission.Name} deleted", 
                              "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                SelectedPermission = null;
                
                // Обновляем отображение, если выбран пользователь
                if (SelectedUser != null)
                {
                    LoadUserPermissions();
                }
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync("System", "PermissionDeleteError", 
                                            $"Error deleting permission: {ex.Message}");
            
            MessageBox.Show("Error deleting permission", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AssignPermission()
    {
        try
        {
            if (SelectedUser == null || SelectedPermission == null)
            {
                MessageBox.Show("Please select both a user and a permission", 
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Проверяем, есть ли уже такое назначенное разрешение
            var existingPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == SelectedUser.Id && up.PermissionId == SelectedPermission.Id);
            
            if (existingPermission != null)
            {
                MessageBox.Show($"User {SelectedUser.Username} already has this permission", 
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var confirmMessage = $"Are you sure you want to assign permission '{SelectedPermission.Name}' to user {SelectedUser.Username}?";
            var result = MessageBox.Show(confirmMessage, "Confirm Permission Assignment", 
                                      MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var userPermission = new UserPermission
                {
                    UserId = SelectedUser.Id,
                    PermissionId = SelectedPermission.Id,
                    IsGranted = true,
                    GrantedDate = DateTime.Now
                };
                
                await _context.UserPermissions.AddAsync(userPermission);
                await _context.SaveChangesAsync();
                
                await _auditService.LogActionAsync(SelectedUser.Id.ToString(), "PermissionAssigned", 
                                                $"Permission {SelectedPermission.Name} assigned to user {SelectedUser.Username}");
                
                MessageBox.Show($"Permission '{SelectedPermission.Name}' assigned to user {SelectedUser.Username}", 
                              "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoadUserPermissions();
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync(SelectedUser?.Id.ToString() ?? "System", "PermissionAssignError", 
                                            $"Error assigning permission: {ex.Message}");
            
            MessageBox.Show("Error assigning permission", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RevokePermission()
    {
        try
        {
            if (SelectedUser == null || SelectedPermission == null)
            {
                MessageBox.Show("Please select both a user and a permission", 
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Проверяем, есть ли такое назначенное разрешение
            var existingPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == SelectedUser.Id && up.PermissionId == SelectedPermission.Id);
            
            if (existingPermission == null)
            {
                MessageBox.Show($"User {SelectedUser.Username} does not have this permission", 
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var confirmMessage = $"Are you sure you want to revoke permission '{SelectedPermission.Name}' from user {SelectedUser.Username}?";
            var result = MessageBox.Show(confirmMessage, "Confirm Permission Revocation", 
                                      MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _context.UserPermissions.Remove(existingPermission);
                await _context.SaveChangesAsync();
                
                await _auditService.LogActionAsync(SelectedUser.Id.ToString(), "PermissionRevoked", 
                                                $"Permission {SelectedPermission.Name} revoked from user {SelectedUser.Username}");
                
                MessageBox.Show($"Permission '{SelectedPermission.Name}' revoked from user {SelectedUser.Username}", 
                              "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoadUserPermissions();
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync(SelectedUser?.Id.ToString() ?? "System", "PermissionRevokeError", 
                                            $"Error revoking permission: {ex.Message}");
            
            MessageBox.Show("Error revoking permission", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Search()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                LoadPermissions();
                return;
            }
            
            var filteredPermissions = await _context.Permissions
                .Where(p => p.Name.Contains(_searchText) || p.Description.Contains(_searchText))
                .ToListAsync();
            
            Permissions.Clear();
            foreach (var permission in filteredPermissions)
            {
                Permissions.Add(permission);
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync("System", "SearchError", 
                                            $"Error searching permissions: {ex.Message}");
            
            MessageBox.Show("Error searching permissions", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void FilterUsers()
    {
        try
        {
            if (_selectedRoleFilter == null)
            {
                LoadUsers();
                return;
            }
            
            var filteredUsers = await _context.Users
                .Include(u => u.CustomPermissions)
                .Where(u => u.Role == _selectedRoleFilter)
                .ToListAsync();
            
            Users.Clear();
            foreach (var user in filteredUsers)
            {
                Users.Add(user);
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync("System", "FilterUsersError", 
                                            $"Error filtering users: {ex.Message}");
            
            MessageBox.Show("Error filtering users", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value) && string.IsNullOrWhiteSpace(value))
            {
                LoadPermissions();
            }
        }
    }
}
