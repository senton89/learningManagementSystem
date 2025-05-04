using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthenticationService _authService;
        private string _username;
        private string _password;
        private string _errorMessage;
        private bool _isLoading;
        private bool _showPasswordResetForm;
        private string _resetEmail;
        private User _currentUser;
        private readonly UserService _userService;
        private readonly MainWindowViewModel _mainViewModel;
        private readonly NotificationService _notificationService;
        
        public Visibility PasswordHintVisibility
        {
            get => string.IsNullOrEmpty(Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        public LoginViewModel(AuthenticationService authService, 
            UserService userService, 
            NotificationService notificationService,
            MainWindowViewModel mainViewModel)
        {
            _authService = authService;
            _userService = userService;
            _notificationService = notificationService;
            _mainViewModel = mainViewModel;
            LoginCommand = new RelayCommand(LoginAsync, CanLogin);
            ForgotPasswordCommand = new RelayCommand(DisplayPasswordResetForm);
            ResetPasswordCommand = new RelayCommand(ResetPasswordAsync, CanResetPassword);
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ErrorMessage = string.Empty;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ErrorMessage = string.Empty;
                    OnPropertyChanged(nameof(PasswordHintVisibility)); // Обновляем видимость подсказки
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool ShowPasswordResetForm
        {
            get => _showPasswordResetForm;
            set => SetProperty(ref _showPasswordResetForm, value);
        }

        public string ResetEmail
        {
            get => _resetEmail;
            set
            {
                if (SetProperty(ref _resetEmail, value))
                {
                    ErrorMessage = string.Empty;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public User CurrentUser
        {
            get => _currentUser;
            private set => SetProperty(ref _currentUser, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand ResetPasswordCommand { get; }

        private bool CanLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var (success, message, user) = await _authService.AuthenticateAsync(Username, Password);
                
                if (success)
                {
                    CurrentUser = user;
                    
                    // Устанавливаем текущего пользователя в сервисе
                    await _userService.SetCurrentUserAsync(user.Id);
                
                    // Инициализируем сервис уведомлений
                    await _mainViewModel.InitializeNotificationServiceAsync();
                    
                    _mainViewModel.UpdateAuthenticationState();
                    
                    if (user.Credentials.RequirePasswordChange)
                    {
                        // Show password change dialog
                        _mainViewModel.NavigateToPasswordRecoveryCommand.Execute(user);
                    }
                    else
                    {
                        // Navigate to main screen
                        _mainViewModel.NavigateToDashboardCommand.Execute(null);
                    }
                }
                else
                {
                    ErrorMessage = message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Произошла ошибка при входе в систему";
            }
            finally
            {
                IsLoading = false;
            }
        }
        

        private void DisplayPasswordResetForm()
        {
            ShowPasswordResetForm = true;
            ErrorMessage = string.Empty;
        }

        private bool CanResetPassword()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(ResetEmail);
        }

        private async Task ResetPasswordAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var token = await _authService.GeneratePasswordResetTokenAsync(ResetEmail);
                if (token != null)
                {
                    // TODO: Отправить email с токеном
                    MessageBox.Show("Инструкции по сбросу пароля отправлены на ваш email", 
                        "Сброс пароля", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowPasswordResetForm = false;
                }
                else
                {
                    ErrorMessage = "Пользователь с указанным email не найден";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Произошла ошибка при сбросе пароля";
                // TODO: Логирование ошибки
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
