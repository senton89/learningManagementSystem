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

namespace WpfApp1.ViewModels
{
    public class UserManagementViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        private readonly AuditService _auditService;
        private readonly LmsDbContext _dbContext;
        private User _selectedUser;
        private string _searchText;
        private UserRole? _selectedRoleFilter;
        private bool _showOnlyActive = true;

        public ObservableCollection<User> Users { get; } = new ObservableCollection<User>();
        public ObservableCollection<UserRole> AvailableRoles { get; } = new ObservableCollection<UserRole>();

        public User SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    LoadUsersAsync();
                }
            }
        }

        public UserRole? SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                if (SetProperty(ref _selectedRoleFilter, value))
                {
                    LoadUsersAsync();
                }
            }
        }

        public bool ShowOnlyActive
        {
            get => _showOnlyActive;
            set
            {
                if (SetProperty(ref _showOnlyActive, value))
                {
                    LoadUsersAsync();
                }
            }
        }

        public ICommand CreateUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ViewUserHistoryCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ResetFiltersCommand { get; }

        public UserManagementViewModel(UserService userService, AuditService auditService, LmsDbContext dbContext)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Initialize available roles
            foreach (var role in UserRoleExtensions.AllRoles())
            {
                AvailableRoles.Add(role);
            }

            // Initialize commands
            CreateUserCommand = new RelayCommand(CreateUserAsync);
            EditUserCommand = new RelayCommand(EditUserAsync, CanEditUser);
            DeleteUserCommand = new RelayCommand(DeleteUserAsync, CanDeleteUser);
            ViewUserHistoryCommand = new RelayCommand(ViewUserHistoryAsync, CanViewUserHistory);
            RefreshCommand = new RelayCommand(LoadUsersAsync);
            ResetFiltersCommand = new RelayCommand(ResetFilters);

            // Load users
            LoadUsersAsync();
        }

        private async void LoadUsersAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading users...";

                var query = _dbContext.Users.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(u => 
                        u.Username.Contains(SearchText) || 
                        u.Email.Contains(SearchText) || 
                        u.FirstName.Contains(SearchText) || 
                        u.LastName.Contains(SearchText));
                }

                if (SelectedRoleFilter.HasValue)
                {
                    query = query.Where(u => u.Role == SelectedRoleFilter.Value);
                }

                if (ShowOnlyActive)
                {
                    query = query.Where(u => u.IsActive);
                }

                var users = await query.ToListAsync();

                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                StatusMessage = $"Loaded {users.Count} users";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading users: {ex.Message}";
                MessageBox.Show($"Error loading users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedRoleFilter = null;
            ShowOnlyActive = true;
        }

        private async void CreateUserAsync()
        {
            try
            {
                var userEditViewModel = new UserEditViewModel(_dbContext, _auditService);
                var dialog = new Views.UserEditDialog(userEditViewModel);

                if (dialog.ShowDialog() == true)
                {
                    await _auditService.LogActionAsync(
                        _userService.CurrentUser?.Id.ToString() ?? "System",
                        "UserCreated",
                        $"User {userEditViewModel.Username} created");

                    LoadUsersAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating user: {ex.Message}";
                MessageBox.Show($"Error creating user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditUserAsync()
        {
            try
            {
                var userEditViewModel = new UserEditViewModel(_dbContext, _auditService, SelectedUser);
                var dialog = new Views.UserEditDialog(userEditViewModel);

                if (dialog.ShowDialog() == true)
                {
                    await _auditService.LogActionAsync(
                        _userService.CurrentUser?.Id.ToString() ?? "System",
                        "UserEdited",
                        $"User {SelectedUser.Username} edited");

                    LoadUsersAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error editing user: {ex.Message}";
                MessageBox.Show($"Error editing user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteUserAsync()
        {
            try
            {
                // Check if this is the last administrator
                if (SelectedUser.Role == UserRole.Administrator)
                {
                    var adminCount = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Administrator);
                    if (adminCount <= 1)
                    {
                        MessageBox.Show("Cannot delete the last administrator account.", "Operation Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to delete user {SelectedUser.Username}?\nThis action cannot be undone.",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _userService.DeleteUserAsync(SelectedUser.Id);
                    await _auditService.LogActionAsync(
                        _userService.CurrentUser?.Id.ToString() ?? "System",
                        "UserDeleted",
                        $"User {SelectedUser.Username} deleted");

                    LoadUsersAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting user: {ex.Message}";
                MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewUserHistoryAsync()
        {
            try
            {
                var dialog = new Views.UserHistoryDialog(_auditService, SelectedUser);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing user history: {ex.Message}";
                MessageBox.Show($"Error viewing user history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanEditUser() => SelectedUser != null;
        private bool CanDeleteUser() => SelectedUser != null && SelectedUser.Id != _userService.CurrentUser?.Id;
        private bool CanViewUserHistory() => SelectedUser != null;
    }
}
