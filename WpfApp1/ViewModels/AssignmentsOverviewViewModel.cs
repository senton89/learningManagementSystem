using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class AssignmentsOverviewViewModel : ViewModelBase
    {
        private readonly CourseService _courseService;
        private readonly UserService _userService;
        private ObservableCollection<Assignment> _assignments;
        private Course _selectedCourse;
        
        public ObservableCollection<Course> Courses { get; } = new ObservableCollection<Course>();
        public ObservableCollection<Assignment> Assignments 
        { 
            get => _assignments; 
            set => SetProperty(ref _assignments, value); 
        }
        
        public Course SelectedCourse
        {
            get => _selectedCourse;
            set
            {
                if (SetProperty(ref _selectedCourse, value) && value != null)
                {
                    LoadAssignmentsAsync(value.Id);
                }
            }
        }
        
        public ICommand CreateAssignmentCommand { get; }
        public ICommand EditAssignmentCommand { get; }
        public ICommand DeleteAssignmentCommand { get; }
        public ICommand ReviewAssignmentCommand { get; }
        
        public AssignmentsOverviewViewModel(CourseService courseService, UserService userService)
        {
            _courseService = courseService;
            _userService = userService;
            
            Assignments = new ObservableCollection<Assignment>();
            
            CreateAssignmentCommand = new RelayCommand<Course>(CreateAssignment, c => c != null);
            EditAssignmentCommand = new RelayCommand<Assignment>(EditAssignment, a => a != null);
            DeleteAssignmentCommand = new RelayCommand<Assignment>(DeleteAssignment, a => a != null);
            ReviewAssignmentCommand = new RelayCommand<Assignment>(ReviewAssignment, a => a != null);
            
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
        
        private async void LoadAssignmentsAsync(int courseId)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading assignments...";
                
                var assignments = await _courseService.GetAssignmentsAsync(courseId);
                
                Assignments.Clear();
                foreach (var assignment in assignments)
                {
                    Assignments.Add(assignment);
                }
                
                StatusMessage = $"Loaded {assignments.Count} assignments";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading assignments: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void CreateAssignment(Course course)
        {
            if (course == null) return;
            
            var assignment = new Assignment
            {
                CourseId = course.Id,
                CreatedDate = System.DateTime.UtcNow,
                Title = "New Assignment",
                Description = "Assignment description",
                DueDate = System.DateTime.UtcNow.AddDays(7),
                MaxScore = 100,
                AllowLateSubmissions = true,
                MaxFileSize = 10, // 10 MB by default
                AllowedFileExtensions = new List<string> { ".pdf", ".doc", ".docx", ".txt" }
            };
            
            var dialog = new Views.AssignmentDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new AssignmentViewModel(assignment, _courseService, _userService)
            };
            
            if (dialog.ShowDialog() == true)
            {
                LoadAssignmentsAsync(course.Id);
            }
        }
        
        private void EditAssignment(Assignment assignment)
        {
            if (assignment == null) return;
            
            var dialog = new Views.AssignmentDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new AssignmentViewModel(assignment, _courseService, _userService)
            };
            
            if (dialog.ShowDialog() == true)
            {
                LoadAssignmentsAsync(assignment.CourseId);
            }
        }
        
        private async void DeleteAssignment(Assignment assignment)
        {
            if (assignment == null) return;
            
            var result = MessageBox.Show(
                "Are you sure you want to delete this assignment? This action cannot be undone.",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsBusy = true;
                    StatusMessage = "Deleting assignment...";
                    
                    await _courseService.DeleteAssignmentAsync(assignment.Id);
                    
                    LoadAssignmentsAsync(assignment.CourseId);
                    StatusMessage = "Assignment deleted successfully";
                }
                catch (System.Exception ex)
                {
                    StatusMessage = $"Error deleting assignment: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
        
        private void ReviewAssignment(Assignment assignment)
        {
            if (assignment == null) return;
            
            var dialog = new Views.AssignmentReviewDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new AssignmentReviewViewModel(_courseService, assignment)
            };
            
            dialog.ShowDialog();
        }
    }
}