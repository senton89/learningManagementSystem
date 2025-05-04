using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels;

public interface INavigationService
{
    void NavigateTo(ViewModelBase viewModel);
}
public class DashboardViewModel : ViewModelBase
{
    private readonly CourseService _courseService;
    private readonly UserService _userService;
    private readonly NotificationService _notificationService;
    private readonly MainWindowViewModel _mainViewModel;
    
    public ObservableCollection<Course> EnrolledCourses { get; } = new();
    public ObservableCollection<AssignmentViewModel> UpcomingAssignments { get; } = new();
    
    public ICommand OpenCourseCommand { get; }
    public ICommand OpenAssignmentCommand { get; }
    
    public ICommand ReviewSubmissionCommand { get; }
    
    public DashboardViewModel(CourseService courseService, UserService userService, NotificationService notificationService, MainWindowViewModel mainViewModel)
    {
        _courseService = courseService;
        _userService = userService;
        _notificationService = notificationService;
        _mainViewModel = mainViewModel;

        OpenCourseCommand = new RelayCommand<Course>(OpenCourse);
        OpenAssignmentCommand = new RelayCommand<AssignmentViewModel>(OpenAssignment);
        ReviewSubmissionCommand = new RelayCommand<AssignmentSubmission>(ReviewSubmission);
        
        // Load data when created
        LoadDataAsync();
    }
    
    public async Task LoadCoursesAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Загрузка данных...";

            // Load enrolled courses
            var courses = await _courseService.GetCoursesAsync(CourseFilter.Enrolled);
            EnrolledCourses.Clear();
            foreach (var course in courses)
            {
                EnrolledCourses.Add(course);
            }

            StatusMessage = "Данные загружены";
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка при загрузке данных: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async void LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Загрузка данных...";
            
            CourseFilter filter = CourseFilter.Enrolled;
            if (_userService.CurrentUser.Role == UserRole.Teacher || 
                _userService.CurrentUser.Role == UserRole.Administrator)
            {
                filter = CourseFilter.Teaching;
            }
            
            // Загружаем курсы, на которые записан студент
            var courses = await _courseService.GetCoursesAsync(filter);
            EnrolledCourses.Clear();
            foreach (var course in courses)
            {
                EnrolledCourses.Add(course);
            }
            
            // Загружаем предстоящие задания
            UpcomingAssignments.Clear();
            foreach (var course in courses)
            {
                var assignments = await _courseService.GetAssignmentsAsync(course.Id);
                foreach (var assignment in assignments)
                {
                    // Добавляем только те задания, которые еще не выполнены и не просрочены
                    if (assignment.DueDate > DateTime.Now)
                    {
                        var submission = await _courseService.GetLastSubmissionAsync(assignment.Id, _userService.CurrentUser.Id);
                        if (submission == null || submission.Status == SubmissionStatus.Draft)
                        {
                            UpcomingAssignments.Add(new AssignmentViewModel(assignment, _courseService, _userService));
                        }
                    }
                }
            }
            
            StatusMessage = "Данные загружены";
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка при загрузке данных: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void ReviewSubmission(AssignmentSubmission submission)
    {
        if (_mainViewModel != null && submission?.Assignment != null)
        {
            _mainViewModel.NavigateToAssignmentReviewCommand.Execute(submission.Assignment);
        }
    }
    
    private void OpenCourse(Course course)
    {
        // Переходим к просмотру курса
        if (_mainViewModel != null)
        {
            var courseViewModel = new CourseViewModel(course, _courseService, _userService);
            _mainViewModel.NavigateToCourse(courseViewModel);
        }
    }
    
    private void OpenAssignment(AssignmentViewModel assignment)
    {
        // Открываем задание
        if (_mainViewModel != null)
        {
            _mainViewModel.NavigateToAssignment(assignment);
        }
    }
}