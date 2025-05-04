using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class UserEditViewModel : NotifyPropertyChangedBase, IDataErrorInfo
    {
        private readonly LmsDbContext _dbContext;
        private readonly AuditService _auditService;
        private readonly User _user;
        private bool _isNewUser;
        private string _username;
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private UserRole _selectedRole;
        private bool _isActive = true;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public UserRole SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
        
        public bool IsNewUser => _isNewUser;

        public ObservableCollection<UserRole> AvailableRoles { get; } = new ObservableCollection<UserRole>();

        public string DialogTitle => IsNewUser ? "Create New User" : "Edit User";

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public UserEditViewModel(LmsDbContext dbContext, AuditService auditService, User user = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            
            _isNewUser = user == null;
            _user = user ?? new User();

            // Initialize properties from user if editing
            if (!IsNewUser)
            {
                Username = _user.Username;
                FirstName = _user.FirstName;
                LastName = _user.LastName;
                Email = _user.Email;
                SelectedRole = _user.Role;
                IsActive = _user.IsActive;
                
            }
            else
            {
                // Default values for new user
                SelectedRole = UserRole.Student;
                IsActive = true;
                
            }

            // Initialize available roles
            foreach (var role in UserRoleExtensions.AllRoles())
            {
                AvailableRoles.Add(role);
            }

            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private async void SaveAsync()
        {
            try
            {
                if (IsNewUser)
                {
                    // Create new user
                    var user = new User
                    {
                        Username = Username,
                        FirstName = FirstName,
                        LastName = LastName,
                        Email = Email,
                        Role = SelectedRole,
                        IsActive = IsActive,
                        RegistrationDate = DateTime.UtcNow
                    };

                    // Create credentials
                    var authService = new AuthenticationService(_dbContext, _auditService);
                    var (hash, salt) = authService.HashPassword(Password);
                    
                    var credentials = new UserCredentials
                    {
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        PasswordChangedDate = DateTime.UtcNow,
                        RequirePasswordChange = false,
                        TwoFactorSecretKey = string.Empty,
                        IsTwoFactorEnabled = false,
                        PasswordResetToken = salt + "-" + hash
                    };

                    user.Credentials = credentials;

                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync();

                    await _auditService.LogActionAsync(
                        "System",
                        "UserCreated",
                        $"User {user.Username} created with role {user.Role}");
                }
                else
                {
                    // Update existing user
                    _user.FirstName = FirstName;
                    _user.LastName = LastName;
                    _user.Email = Email;
                    
                    var oldRole = _user.Role;
                    _user.Role = SelectedRole;
                    _user.IsActive = IsActive;

                    // Update password if provided
                    if (!string.IsNullOrEmpty(Password))
                    {
                        var authService = new AuthenticationService(_dbContext, _auditService);
                        var (hash, salt) = authService.HashPassword(Password);
                        
                        if (_user.Credentials == null)
                        {
                            _user.Credentials = new UserCredentials
                            {
                                UserId = _user.Id,
                                PasswordHash = hash,
                                PasswordSalt = salt,
                                PasswordChangedDate = DateTime.UtcNow,
                                RequirePasswordChange = false,
                                TwoFactorSecretKey = string.Empty,
                                IsTwoFactorEnabled = false,
                                PasswordResetToken = salt + "-" + hash
                            };
                        }
                        else
                        {
                            _user.Credentials.PasswordHash = hash;
                            _user.Credentials.PasswordSalt = salt;
                            _user.Credentials.PasswordChangedDate = DateTime.UtcNow;
                        }
                    }

                    _dbContext.Users.Update(_user);
                    await _dbContext.SaveChangesAsync();

                    // Log role change if role was changed
                    if (oldRole != SelectedRole)
                    {
                        await _auditService.LogRoleChangeAsync(_user, oldRole, SelectedRole, "System");
                    }

                    await _auditService.LogActionAsync(
                        _user.Id.ToString(),
                        "UserUpdated",
                        $"User {_user.Username} updated");
                }

                DialogResult = true;
                var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            DialogResult = false;
    
            // Find the window and close it
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        private bool CanSave()
        {
            return !HasErrors && 
                   !string.IsNullOrWhiteSpace(Username) && 
                   !string.IsNullOrWhiteSpace(FirstName) && 
                   !string.IsNullOrWhiteSpace(LastName) && 
                   !string.IsNullOrWhiteSpace(Email) &&
                   (IsNewUser ? !string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(ConfirmPassword) : true);
        }

        #region Validation

        public string this[string columnName]
        {
            get
            {
                string error = null;

                switch (columnName)
                {
                    case nameof(Username):
                        if (string.IsNullOrWhiteSpace(Username))
                            error = "Username is required";
                        else if (Username.Length < 3)
                            error = "Username must be at least 3 characters";
                        break;

                    case nameof(FirstName):
                        if (string.IsNullOrWhiteSpace(FirstName))
                            error = "First name is required";
                        break;

                    case nameof(LastName):
                        if (string.IsNullOrWhiteSpace(LastName))
                            error = "Last name is required";
                        break;

                    case nameof(Email):
                        if (string.IsNullOrWhiteSpace(Email))
                            error = "Email is required";
                        else if (!new EmailAddressAttribute().IsValid(Email))
                            error = "Invalid email format";
                        break;

                    case nameof(Password):
                        if (IsNewUser && string.IsNullOrWhiteSpace(Password))
                            error = "Password is required for new users";
                        else if (!string.IsNullOrWhiteSpace(Password) && Password.Length < 6)
                            error = "Password must be at least 6 characters";
                        break;

                    case nameof(ConfirmPassword):
                        if (IsNewUser && string.IsNullOrWhiteSpace(ConfirmPassword))
                            error = "Please confirm password";
                        else if (!string.IsNullOrWhiteSpace(Password) && Password != ConfirmPassword)
                            error = "Passwords do not match";
                        break;
                }

                return error;
            }
        }

        public string Error => null;

        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(Username)]) ||
                       !string.IsNullOrEmpty(this[nameof(FirstName)]) ||
                       !string.IsNullOrEmpty(this[nameof(LastName)]) ||
                       !string.IsNullOrEmpty(this[nameof(Email)]) ||
                       !string.IsNullOrEmpty(this[nameof(Password)]) ||
                       !string.IsNullOrEmpty(this[nameof(ConfirmPassword)]);
            }
        }

        #endregion

        #region Dialog

        public bool? DialogResult { get; set; }

        public void Close()
        {
            if (Window != null)
            {
                Window.DialogResult = DialogResult;
                Window.Close();
            }
        }

        private Window Window => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);

        #endregion
    }
}
