using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1.ViewModels;

public partial class RegistrationViewModel : ViewModelBase
{
    private readonly LmsDbContext _dbContext;

    [ObservableProperty]
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [MinLength(3, ErrorMessage = "Имя пользователя должно содержать не менее 3 символов")]
    private string _username = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Неверный формат email")]
    private string _email = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать не менее 6 символов")]
    private string _password = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Подтверждение пароля обязательно")]
    private string _confirmPassword = string.Empty;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    public RegistrationViewModel(LmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [RelayCommand]
    private async Task Register()
    {
        // Очистка предыдущих ошибок
        ClearErrors();
        
        ValidateAllProperties();
        
        if (HasErrors)
        {
            StatusMessage = string.Join("\n", GetErrors().Select(e => e.ErrorMessage));
            return;
        }

        // Проверка совпадения паролей
        if (Password != ConfirmPassword)
        {
            StatusMessage = "Пароли не совпадают";
            return;
        }


        IsBusy = true;
        StatusMessage = "Регистрация...";

        try
        {
            if (await _dbContext.Users.AnyAsync(u => u.Username == Username))
            {
                StatusMessage = "Пользователь с таким именем уже существует";
                return;
            }

            if (await _dbContext.Users.AnyAsync(u => u.Email == Email))
            {
                StatusMessage = "Email уже зарегистрирован";
                return;
            }
            
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(Password, salt);

            // Create user
            var user = new User {
                Username = Username,
                Email = Email,
                PasswordHash = passwordHash,
                FirstName = Username,
                LastName = Username,
                Role = UserRole.Student,
                RegistrationDate = DateTime.UtcNow
            };
        
            // Create credentials
            var credentials = new UserCredentials {
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                PasswordChangedDate = DateTime.UtcNow,
                RequirePasswordChange = false,
                TwoFactorSecretKey = string.Empty,
                IsTwoFactorEnabled = false,
                PasswordResetToken = salt + "-" + passwordHash
            };
        
            // Link credentials to user
            user.Credentials = credentials;
        
            // Add user to context
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        
            StatusMessage = "Registration successful! Please login.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Registration failed: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
