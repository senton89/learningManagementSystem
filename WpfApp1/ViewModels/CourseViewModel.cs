using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Views;

namespace WpfApp1.ViewModels;


public class CourseViewModel : ViewModelBase
{
    private readonly CourseService _courseService;
    private readonly UserService _userService;
    private readonly Course _course;
    
    public string Title => _course.Title;
    public string Description => _course.Description;
    public string TeacherName => _course.Teacher?.FullName ?? "Не назначен";
    public bool IsTeacher => _userService.CurrentUser?.Role == UserRole.Teacher || 
                             _userService.CurrentUser?.Role == UserRole.Administrator;
    private DateTime? _startDate;
    public DateTime? StartDate 
    { 
        get => _course.StartDate; 
        set 
        {
            if (_course.StartDate != value)
            {
                _course.StartDate = value;
                OnPropertyChanged(nameof(StartDate));
            }
        }
    }
    private DateTime? _endDate;
    public DateTime? EndDate 
    { 
        get => _course.EndDate; 
        set 
        {
            if (_course.EndDate != value)
            {
                _course.EndDate = value;
                OnPropertyChanged(nameof(EndDate));
            }
        }
    }
    
    public ObservableCollection<ModuleViewModel> Modules { get; } = new();
    
    public ICommand OpenModuleCommand { get; }
    public ICommand AddContentCommand { get; }
    public ICommand OpenContentCommand { get; }

    public CourseViewModel(Course course, CourseService courseService, UserService userService)
    {
        _course = course;
        _courseService = courseService;
        _userService = userService;

        // Initialize commands
        AddContentCommand = new RelayCommand(AddContent);
        OpenModuleCommand = new RelayCommand<ModuleViewModel>(OpenModule);
        OpenContentCommand = new RelayCommand<Content>(OpenContent);

        // Load course modules
        LoadModulesAsync();
    }
    
    private async void OpenContent(Content content)
{
    try
    {
        IsBusy = true;
        
        switch (content.Type)
        {
            case ContentType.Text:
                var textDialog = new TextContentDialog();
                textDialog.DataContext = new TextContentViewModel(content);
                textDialog.ShowDialog();
                break;
                
            case ContentType.Video:
                var videoDialog = new VideoContentDialog();
                videoDialog.Loaded += (s, e) => {
                    var mediaElement = videoDialog.FindName("VideoPlayer") as MediaElement;
                    videoDialog.DataContext = new VideoContentViewModel(content, mediaElement);
                };
                videoDialog.ShowDialog();
                break;
                
            case ContentType.Quiz:
                try
                {
                    var quiz = await _courseService.GetQuizAsync(content.Id);
                    var quizDialog = new QuizDialog(quiz);
                    quizDialog.DataContext = new QuizViewModel(_courseService, _userService.CurrentUser, content);
                    quizDialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                break;
                
            case ContentType.Assignment:
                try
                {
                    var assignment = await _courseService.GetAssignmentAsync(content.Id);
                    var assignmentDialog = new AssignmentDialog();
                    assignmentDialog.DataContext = new AssignmentViewModel(assignment, _courseService, _userService);
                    assignmentDialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке задания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                break;
        }
        
        // Обновляем прогресс студента
        if (_userService.CurrentUser.Role == UserRole.Student)
        {
            var progress = new StudentProgress
            {
                UserId = _userService.CurrentUser.Id,
                ContentId = content.Id,
                Status = ProgressStatus.InProgress,
                StartDate = DateTime.UtcNow
            };
            
            await _courseService.UpdateProgressAsync(progress);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Ошибка при открытии контента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
        IsBusy = false;
    }
}

    private async void AddContent()
    {
        if (!IsTeacher) return;
    
        // Get the selected module or first module
        var selectedModule = Modules.FirstOrDefault(m => m.IsExpanded) ?? Modules.FirstOrDefault();
        if (selectedModule == null) return;
    
        // Create a new content
        var content = new Content
        {
            Title = "New Content",
            Type = ContentType.Text,
            Data = "Enter content here",
            ModuleId = selectedModule.Id
        };
    
        // Show content edit dialog
        var dialog = new ContentEditDialog(content, selectedModule.Id, _courseService);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _courseService.AddContentAsync(selectedModule.Id, content, _userService.CurrentUser.Id);
                LoadModulesAsync();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Только преподаватель курса может добавлять контент.", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении контента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private async void LoadModulesAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Загрузка модулей...";
            
            // Загружаем полную информацию о курсе с модулями
            var fullCourse = await _courseService.GetCourseAsync(_course.Id);
            
            Modules.Clear();
            if (fullCourse.Modules != null)
            {
                foreach (var module in fullCourse.Modules.OrderBy(m => m.OrderIndex))
                {
                    var moduleViewModel = new ModuleViewModel(module);
                
                    Modules.Add(moduleViewModel);
                }
            }
            
            StatusMessage = "Модули загружены";
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка при загрузке модулей: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void OpenModule(ModuleViewModel module)
    {
        // Переключаем состояние раскрытия модуля
        module.IsExpanded = !module.IsExpanded;
    }
}
