using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Views;

namespace WpfApp1.ViewModels
{
    public class CourseManagementViewModel : ViewModelBase
    {
        private readonly CourseService _courseService;
        private UserService _userService;
        private readonly User _currentUser;
        private Course _selectedCourse;
        private bool _isLoading;
        private string _searchQuery;
        private CourseFilter _currentFilter;
        
        public ObservableCollection<Course> Courses { get; } = new ObservableCollection<Course>();

        public Course SelectedCourse
        {
            get => _selectedCourse;
            set
            {
                if (SetProperty(ref _selectedCourse, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    RefreshAsync();
                }
            }
        }

        public CourseFilter CurrentFilter
        {
            get => _currentFilter;
            set
            {
                if (value != _currentFilter)
                {
                    _currentFilter = value;
                    OnPropertyChanged(nameof(CurrentFilter));
                }
            }
        }

        public ICommand AddCourseCommand { get; }
        public ICommand EditCourseCommand { get; }
        public ICommand DeleteCourseCommand { get; }
        public ICommand EnrollCommand { get; }
        public ICommand ViewProgressCommand { get; }
        public ICommand RefreshCommand { get; }
        
        public CourseManagementViewModel(CourseService courseService, UserService userService, User currentUser)
        {
            _courseService = courseService;
            _userService = userService; 
            _currentUser = currentUser;
            
            Courses = new ObservableCollection<Course>();
            
            AddCourseCommand = new RelayCommand(AddCourseAsync, () => !IsLoading && _currentUser.IsTeacher());
            EditCourseCommand = new RelayCommand(EditCourseAsync, CanEditCourse);
            DeleteCourseCommand = new RelayCommand(DeleteCourseAsync, CanDeleteCourse);
            EnrollCommand = new RelayCommand(EnrollAsync, CanEnroll);
            ViewProgressCommand = new RelayCommand(ViewProgressAsync, CanViewProgress);
            RefreshCommand = new RelayCommand(RefreshAsync);
            
            LoadCoursesAsync();
        }
        

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        

        private async void LoadCoursesAsync()
        {
            try
            {
                IsLoading = true;
                Courses.Clear();

                var courses = await _courseService.GetCoursesAsync(_currentFilter, _searchQuery);
                foreach (var course in courses)
                {
                    Courses.Add(course);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке курсов: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void AddCourseAsync()
        {
            try
            {
                var viewModel = new CourseEditViewModel(_courseService, _currentUser);
                var dialog = new CourseEditDialog(viewModel);
                if (dialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    await RefreshAsync();
                    MessageBox.Show("Курс успешно создан", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании курса: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void EditCourseAsync()
        {
            try
            {
                var viewModel = new CourseEditViewModel(_courseService, _currentUser, SelectedCourse);
                var dialog = new CourseEditDialog(viewModel);
                
                if (dialog.ShowDialog() == true)
                {
                    IsBusy = true;
                    await RefreshAsync();
                    MessageBox.Show("Курс успешно обновлен", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении курса: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        public void OnFilterSelected(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Tag is CourseFilter filter)
                {
                    CurrentFilter = filter;
                }
            }
        }

        private async void DeleteCourseAsync()
        {
            try
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить этот курс?", 
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    await _courseService.DeleteCourseAsync(SelectedCourse.Id, _currentUser.Id);
                    Courses.Remove(SelectedCourse);
                    MessageBox.Show("Курс успешно удален", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении курса: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void EnrollAsync()
        {
            try
            {
                IsLoading = true;
                await _courseService.EnrollStudentAsync(SelectedCourse.Id, _currentUser.Id);
                await RefreshAsync();
                MessageBox.Show("Вы успешно записались на курс", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при записи на курс: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ViewProgressAsync()
        {
            try
            {
                var progress = await _courseService.GetStudentProgressAsync(SelectedCourse.Id, _currentUser.Id);
                var viewModel = new CourseProgressViewModel(_courseService, _currentUser, SelectedCourse);
                var dialog = new CourseProgressDialog();
                dialog.DataContext = viewModel;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке прогресса: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                var courses = await _courseService.GetCoursesAsync(_currentFilter, _searchQuery);
        
                // Switch back to UI thread before modifying the collection asynchronously
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    Courses.Clear();
                    foreach (var course in courses)
                    {
                        Courses.Add(course);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке курсов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanEditCourse()
        {
            return !IsLoading && SelectedCourse != null && 
                   (SelectedCourse.Teacher?.Id == _currentUser.Id || _currentUser.IsAdmin());
        }

        private bool CanDeleteCourse()
        {
            return !IsLoading && SelectedCourse != null && 
                   (SelectedCourse.Teacher?.Id == _currentUser.Id || _currentUser.IsAdmin());
        }

        private bool CanEnroll()
        {
            return !IsLoading && SelectedCourse != null && 
                   !_currentUser.IsTeacher() && !_currentUser.IsAdmin() &&
                   SelectedCourse.Students?.Any(s => s.Id == _currentUser.Id) != true;
        }

        private bool CanViewProgress()
        {
            return !IsLoading && SelectedCourse != null &&
                   SelectedCourse.Students?.Any(s => s.Id == _currentUser.Id) == true;
        }
    }

    public enum CourseFilter
    {
        All,
        Teaching,
        Enrolled,
        Available
    }
}
