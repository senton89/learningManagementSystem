using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class ProgressOverviewViewModel : ViewModelBase
    {
        private readonly CourseService _courseService;
        private readonly UserService _userService;
        private ObservableCollection<StudentProgressViewModel> _studentProgress;
        private Course _selectedCourse;
        private User _selectedStudent;
        
        public ObservableCollection<Course> Courses { get; } = new ObservableCollection<Course>();
        public ObservableCollection<User> Students { get; } = new ObservableCollection<User>();
        
        public ObservableCollection<StudentProgressViewModel> StudentProgress
        {
            get => _studentProgress;
            set => SetProperty(ref _studentProgress, value);
        }
        
        public Course SelectedCourse
        {
            get => _selectedCourse;
            set
            {
                if (SetProperty(ref _selectedCourse, value) && value != null)
                {
                    LoadStudentsAsync(value.Id);
                }
            }
        }
        
        public User SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (SetProperty(ref _selectedStudent, value) && value != null && SelectedCourse != null)
                {
                    LoadStudentProgressAsync(SelectedCourse.Id, value.Id);
                }
            }
        }
        
        public ProgressOverviewViewModel(CourseService courseService, UserService userService)
        {
            _courseService = courseService;
            _userService = userService;
            
            StudentProgress = new ObservableCollection<StudentProgressViewModel>();
            
            LoadCoursesAsync();
        }
        
        private async void LoadCoursesAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading courses...";
                
                var filter = _userService.CurrentUser.Role == UserRole.Teacher ? 
                    CourseFilter.Teaching : CourseFilter.All;
                    
                var courses = await _courseService.GetCoursesAsync(filter);
                
                Courses.Clear();
                foreach (var course in courses)
                {
                    Courses.Add(course);
                }
                
                if (Courses.Any())
                {
                    SelectedCourse = Courses.First();
                }
                
                StatusMessage = $"Loaded {courses.Count} courses";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading courses: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async void LoadStudentsAsync(int courseId)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading students...";
                
                var course = await _courseService.GetCourseAsync(courseId);
                
                Students.Clear();
                if (course.Students != null)
                {
                    foreach (var student in course.Students)
                    {
                        Students.Add(student);
                    }
                }
                
                if (Students.Any())
                {
                    SelectedStudent = Students.First();
                }
                
                StatusMessage = $"Loaded {Students.Count} students";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading students: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async void LoadStudentProgressAsync(int courseId, int studentId)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading progress data...";
                
                var progress = await _courseService.GetStudentProgressAsync(courseId, studentId);
                
                StudentProgress.Clear();
                
                // Group by module
                var moduleGroups = progress
                    .GroupBy(p => p.Content.Module)
                    .OrderBy(g => g.Key.OrderIndex);
                
                foreach (var moduleGroup in moduleGroups)
                {
                    var moduleProgress = new StudentProgressViewModel
                    {
                        ModuleName = moduleGroup.Key.Title,
                        ModuleDescription = moduleGroup.Key.Description,
                        CompletedItems = moduleGroup.Count(p => p.Status == ProgressStatus.Completed),
                        TotalItems = moduleGroup.Count(),
                        ContentItems = new ObservableCollection<ContentProgressItem>(
                            moduleGroup.OrderBy(p => p.Content.OrderIndex)
                                      .Select(p => new ContentProgressItem
                                      {
                                          ContentTitle = p.Content.Title,
                                          ContentType = p.Content.Type,
                                          Status = p.Status,
                                          Score = p.Score,
                                          StartDate = p.StartDate,
                                          CompletionDate = p.CompletionDate
                                      }))
                    };
                    
                    StudentProgress.Add(moduleProgress);
                }
                
                StatusMessage = "Progress data loaded successfully";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading progress data: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    public class StudentProgressViewModel : NotifyPropertyChangedBase
    {
        private string _moduleName;
        private string _moduleDescription;
        private int _completedItems;
        private int _totalItems;
        private ObservableCollection<ContentProgressItem> _contentItems;
        
        public string ModuleName
        {
            get => _moduleName;
            set => SetProperty(ref _moduleName, value);
        }
        
        public string ModuleDescription
        {
            get => _moduleDescription;
            set => SetProperty(ref _moduleDescription, value);
        }
        
        public int CompletedItems
        {
            get => _completedItems;
            set => SetProperty(ref _completedItems, value);
        }
        
        public int TotalItems
        {
            get => _totalItems;
            set => SetProperty(ref _totalItems, value);
        }
        
        public double ProgressPercentage => TotalItems > 0 ? 
            (double)CompletedItems / TotalItems * 100 : 0;
            
        public ObservableCollection<ContentProgressItem> ContentItems
        {
            get => _contentItems;
            set => SetProperty(ref _contentItems, value);
        }
    }
    
    public class ContentProgressItem : NotifyPropertyChangedBase
    {
        private string _contentTitle;
        private ContentType _contentType;
        private ProgressStatus _status;
        private int? _score;
        private DateTime? _startDate;
        private DateTime? _completionDate;
        
        public string ContentTitle
        {
            get => _contentTitle;
            set => SetProperty(ref _contentTitle, value);
        }
        
        public ContentType ContentType
        {
            get => _contentType;
            set => SetProperty(ref _contentType, value);
        }
        
        public ProgressStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        
        public int? Score
        {
            get => _score;
            set => SetProperty(ref _score, value);
        }
        
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }
        
        public DateTime? CompletionDate
        {
            get => _completionDate;
            set => SetProperty(ref _completionDate, value);
        }
        
        public bool IsCompleted => Status == ProgressStatus.Completed;
    }
}