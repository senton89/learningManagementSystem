using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.ViewModels;

namespace WpfApp1.Views;

public partial class RoleManagementDialog : Window 
{ 
    private readonly RoleManagementViewModel _viewModel;

    public RoleManagementDialog(LmsDbContext context, AuditService auditService, IDbContextFactory<LmsDbContext> contextFactory) 
    { 
        InitializeComponent(); 
        _viewModel = new RoleManagementViewModel(context, auditService, contextFactory); 
        DataContext = _viewModel; 
    }
    
    private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) 
    { 
        if (sender is ComboBox comboBox && comboBox.DataContext is User user && e.AddedItems.Count > 0) 
        { 
            var newRole = (UserRole)e.AddedItems[0]; 
            if (user.Role != newRole) 
            { 
                _viewModel.UpdateUserRole(user, newRole); 
            } 
        } 
    } 
}