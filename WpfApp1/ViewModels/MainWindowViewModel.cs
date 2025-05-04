using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Uwp.Notifications;
using WpfApp1.Models;
using WpfApp1.Services;
using RelayCommand = WpfApp1.Infrastructure.RelayCommand;
using ToastArguments = CommunityToolkit.WinUI.Notifications.ToastArguments;

namespace WpfApp1.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LmsDbContext _dbContext;
    private readonly IDbContextFactory<LmsDbContext> _contextFactory;
    private readonly CourseService _courseService;
    private readonly UserService _userService;
    private readonly NotificationService _notificationService;
    private readonly AuthenticationService _authService;
    
    [ObservableProperty]
    private ViewModelBase? _currentViewModel;
    
    [ObservableProperty]
    private Course? _selectedCourse;
    
    [ObservableProperty]
    private bool isAuthenticated;

    [ObservableProperty]
    private bool isAdministrator;

    [ObservableProperty]
    private bool isTeacher;

    [ObservableProperty]
    private bool isTeacherOrAdmin;
    
    public ICommand CreateAssignmentCommandVM { get; private set; }
    public ICommand EditAssignmentCommandVM { get; private set; }
    public ICommand DeleteAssignmentCommandVM { get; private set; }
    public ICommand ViewAssignmentCommandVM { get; private set; }
    public ICommand ReviewAssignmentsCommandVM { get; private set; }

// Добавляем необходимые сервисы в конструктор
    public MainWindowViewModel(
        LmsDbContext dbContext,
        CourseService courseService,
        UserService userService,
        NotificationService notificationService,
        IDbContextFactory<LmsDbContext> contextFactory)
    {
        _dbContext = dbContext;
        _contextFactory = contextFactory;
        _courseService = courseService;
        _userService = userService;
        _notificationService = notificationService;
        _authService = new AuthenticationService(dbContext, new AuditService(contextFactory));

        // Инициализация команд
        InitializeAssignmentCommands();

        // Запускаем сервис уведомлений
        // _notificationService.Start();

        // Регистрируем обработчик уведомлений
        ToastNotificationManagerCompat.OnActivated += HandleNotificationActivation;

        NavigateToLogin();
    }

    public void UpdateAuthenticationState()
    {
        IsAuthenticated = _userService.CurrentUser != null;
        IsAdministrator = _userService.CurrentUser?.Role == UserRole.Administrator;
        IsTeacher = _userService.CurrentUser?.Role == UserRole.Teacher;
        IsTeacherOrAdmin = IsTeacher || IsAdministrator;
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(IsTeacher));
        OnPropertyChanged(nameof(IsTeacherOrAdmin));
    }

    public async Task InitializeNotificationServiceAsync()
    {
        if (_userService.CurrentUser != null)
        {
            await _notificationService.InitializeAsync();
            _notificationService.Start();
        }
    }
    
    [RelayCommand]
    private void NavigateToUserManagement()
    {
        if (_userService.CurrentUser?.Role == UserRole.Administrator)
        {
            var viewModel = new UserManagementViewModel(_userService, new AuditService(_contextFactory), _dbContext);
            CurrentViewModel = viewModel;
        }
        else
        {
            MessageBox.Show("You don't have permission to access user management", 
                "Access Denied", 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
        }
    }
    
    [RelayCommand]
    private void NavigateToCourseManagement()
    {
        var viewModel = new CourseManagementViewModel(_courseService, _userService, _userService.CurrentUser);
        CurrentViewModel = viewModel;
    }
    [RelayCommand]
    private void NavigateToRoleManagement()
    {
        if (_userService.CurrentUser?.Role == UserRole.Administrator)
        {
            var roleManagementDialog = new RoleManagementDialog(_dbContext, new AuditService(_contextFactory), _contextFactory);
            roleManagementDialog.Owner = Application.Current.MainWindow;
            roleManagementDialog.ShowDialog();
        }
        else
        {
            MessageBox.Show("You don't have permission to access role management", 
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async void OpenProfile()
    {
        if (_userService.CurrentUser != null)
        {
            var profileViewModel = new UserProfileViewModel(_userService.CurrentUser, _userService);
            var profileDialog = new UserProfileDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = profileViewModel
            };
        
            if (profileDialog.ShowDialog() == true)
            {
                try
                {
                    await profileViewModel.SaveChangesAsync();
                    StatusMessage = "Profile updated successfully";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error updating profile: {ex.Message}";
                }
            }
        }
    }
    
     [RelayCommand]
        private void NavigateToAssignments()
        {
            if (_userService.CurrentUser != null && 
                (_userService.CurrentUser.Role == UserRole.Teacher || 
                 _userService.CurrentUser.Role == UserRole.Administrator))
            {
                // Create AssignmentsOverviewViewModel
                var viewModel = new AssignmentsOverviewViewModel(_courseService, _userService);
                CurrentViewModel = viewModel;
            }
            else
            {
                MessageBox.Show("You don't have permission to access assignments management", 
                               "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void NavigateToProgress()
        {
            if (_userService.CurrentUser != null && 
                (_userService.CurrentUser.Role == UserRole.Teacher || 
                 _userService.CurrentUser.Role == UserRole.Administrator))
            {
                // Create ProgressOverviewViewModel
                var viewModel = new ProgressOverviewViewModel(_courseService, _userService);
                CurrentViewModel = viewModel;
            }
            else
            {
                MessageBox.Show("You don't have permission to access progress tracking", 
                               "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            if (_userService.CurrentUser?.Role == UserRole.Administrator)
            {
                // Create SystemSettingsViewModel
                var viewModel = new SystemSettingsViewModel(_dbContext);
                CurrentViewModel = viewModel;
            }
            else
            {
                MessageBox.Show("You don't have permission to access system settings",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
    private void OpenNotificationSettings()
    {
        if (_userService.CurrentUser != null)
        {
            var viewModel = new NotificationSettingsViewModel(_notificationService, _courseService, _userService);
            var dialog = new NotificationSettingsDialog(viewModel)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _userService.ClearCurrentUser();
        _notificationService.Stop();
        UpdateAuthenticationState();
        NavigateToLogin();
    }
    
    [RelayCommand]
    private void NavigateToLogin()
    {
        CurrentViewModel = new LoginViewModel(_authService,_userService, _notificationService, this);
    }

    [RelayCommand]
    private void NavigateToRegistration()
    {
        CurrentViewModel = new RegistrationViewModel(_dbContext);
    }

    [RelayCommand]
    private void NavigateToPasswordRecovery()
    {
        CurrentViewModel = new PasswordRecoveryViewModel(_dbContext);
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        var dashboardVM = new DashboardViewModel(_courseService, _userService, _notificationService, this);
        CurrentViewModel = dashboardVM;
    }
    
    public void NavigateToCourse(CourseViewModel courseViewModel)
    {
        CurrentViewModel = courseViewModel;
    }
    
    public void NavigateToAssignment(AssignmentViewModel assignmentViewModel)
    {
        CurrentViewModel = assignmentViewModel;
    }
    
    [RelayCommand]
    public void NavigateToAssignmentReview(Assignment assignment)
    {
        if (_userService.CurrentUser?.Role == UserRole.Teacher || 
            _userService.CurrentUser?.Role == UserRole.Administrator)
        {
            var viewModel = new AssignmentReviewViewModel(_courseService, assignment);
            var dialog = new AssignmentReviewDialog { DataContext = viewModel };
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }
        else
        {
            MessageBox.Show("You don't have permission to review assignments", 
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    
    private async Task LoadCourseDataAsync()
    {
        if (SelectedCourse == null)
            return;
        
        try
        {
            // Reload the course with all its data
            SelectedCourse = await _courseService.GetCourseAsync(SelectedCourse.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading course data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Assignment Management

    private void InitializeAssignmentCommands()
    {
        CreateAssignmentCommandVM = new RelayCommand(CreateAssignment,
            () => SelectedCourse != null && _userService.CurrentUser.HasRole(UserRole.Teacher));
        
        EditAssignmentCommandVM = new Infrastructure.RelayCommand<Assignment>(EditAssignment,
            a => a != null && _userService.CurrentUser.HasRole(UserRole.Teacher));
        
        DeleteAssignmentCommandVM = new Infrastructure.RelayCommand<Assignment>(DeleteAssignment,
            a => a != null && _userService.CurrentUser.HasRole(UserRole.Teacher));
        
        ViewAssignmentCommandVM = new Infrastructure.RelayCommand<Assignment>(ViewAssignment,
            a => a != null);
        
        ReviewAssignmentsCommandVM = new Infrastructure.RelayCommand<Assignment>(ReviewAssignments,
            a => a != null && _userService.CurrentUser.HasRole(UserRole.Teacher));
    }

    private bool CanCreateAssignment() => SelectedCourse != null && _userService.CurrentUser.HasRole(UserRole.Teacher);

    
    [RelayCommand(CanExecute = nameof(CanCreateAssignment))]
    private async void CreateAssignment()
    {
        try
        {
            if (SelectedCourse == null)
                return;
            
            var assignment = new Assignment
            {
                CourseId = SelectedCourse.Id,
                CreatedDate = DateTime.UtcNow,
                Title = "New Assignment",
                Description = "Assignment description",
                DueDate = DateTime.UtcNow.AddDays(7),
                MaxScore = 100, // Добавить установку максимального балла
                AllowLateSubmissions = true,
                MaxFileSize = 10,
                AllowedFileExtensions = new List<string> { ".pdf", ".doc", ".docx", ".txt" }
            };

            var dialog = new AssignmentDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new AssignmentViewModel(assignment, _courseService, _userService)
            };

            if (dialog.ShowDialog() == true)
            {
                await LoadCourseDataAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании задания: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async void EditAssignment(Assignment assignment)
    {
        try
        {
            var dialog = new AssignmentDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new AssignmentViewModel(assignment, _courseService, _userService)
            };

            if (dialog.ShowDialog() == true)
            {
                await LoadCourseDataAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при редактировании задания: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async void DeleteAssignment(Assignment assignment)
    {
        try
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить это задание? Все ответы студентов также будут удалены.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _courseService.DeleteAssignmentAsync(assignment.Id);
                await LoadCourseDataAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении задания: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ViewAssignment(Assignment assignment)
    {
        try
        {
            var dialog = new AssignmentDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new AssignmentViewModel(assignment, _courseService, _userService)
            };

            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии задания: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ReviewAssignments(Assignment assignment)
    {
        try
        {
            var dialog = new AssignmentReviewDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new AssignmentReviewViewModel(_courseService, assignment)
            };

            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии проверки заданий: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
    
    private void HandleNotificationActivation(ToastNotificationActivatedEventArgsCompat e)
    {
        // Получаем аргументы
        var args = ToastArguments.Parse(e.Argument);

        switch (args["action"])
        {
            case "openAssignment":
                if (int.TryParse(args["courseId"], out var courseId) &&
                    int.TryParse(args["assignmentId"], out var assignmentId))
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        try
                        {
                            var course = await _courseService.GetCourseAsync(courseId);
                            var assignment = await _courseService.GetAssignmentAsync(assignmentId);
                            
                            if (course != null && assignment != null)
                            {
                                SelectedCourse = course;
                                ViewAssignment(assignment);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при открытии задания: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }
                break;

            case "openCourse":
                if (int.TryParse(args["courseId"], out courseId))
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        try
                        {
                            var course = await _courseService.GetCourseAsync(courseId);
                            if (course != null)
                            {
                                SelectedCourse = course;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при открытии курса: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }
                break;

            case "openReview":
                if (int.TryParse(args["courseId"], out courseId))
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        try
                        {
                            var course = await _courseService.GetCourseAsync(courseId);
                            if (course != null)
                            {
                                SelectedCourse = course;
                                // Открываем вкладку с заданиями
                                // TODO: Реализовать переключение вкладок
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при открытии проверки: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }
                break;

            case "disableNotifications":
                var type = args["type"];
                // TODO: Реализовать отключение уведомлений определенного типа
                break;
        }
    }
}
