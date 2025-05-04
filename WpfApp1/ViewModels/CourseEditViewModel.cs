using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Views;

namespace WpfApp1.ViewModels
{
    public class CourseEditViewModel : NotifyPropertyChangedBase, IDataErrorInfo
    {
        private readonly CourseService _courseService;
        private readonly User _currentUser;
        private bool _isLoading;
        private Course _course;
        private Module _selectedModule;

        public CourseEditViewModel(CourseService courseService, User currentUser, Course? course = null)
        {
            _courseService = courseService;
            _currentUser = currentUser;
            Course = course ?? new Course 
            { 
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3),
                AccessType = Models.CourseAccessType.Open,
                // MaxStudents = 30
            };
            
            if (Course.Modules == null)
            {
                Course.Modules = new ObservableCollection<Module>();
            }

            Modules = new ObservableCollection<ModuleViewModel>(
                Course.Modules.Select(m => new ModuleViewModel(m)));

            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            AddModuleCommand = new RelayCommand(AddModuleAsync);
            EditModuleCommand = new RelayCommand<ModuleViewModel>(EditModuleAsync);
            DeleteModuleCommand = new RelayCommand<ModuleViewModel>(DeleteModuleAsync);
        }

        public Course Course
        {
            get => _course;
            set
            {
                if (SetProperty(ref _course, value))
                {
                    OnPropertyChanged(nameof(DialogTitle));
                    OnPropertyChanged(nameof(IsNewCourse));
                    OnPropertyChanged(nameof(CourseAccessTypes));
                }
            }
        }

        public Module SelectedModule
        {
            get => _selectedModule;
            set => SetProperty(ref _selectedModule, value);
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

        public bool IsNewCourse => Course?.Id == 0;

        public string DialogTitle => IsNewCourse ? "Создание нового курса" : "Редактирование курса";

        public ICommand SaveCommand { get; }
        public ICommand AddModuleCommand { get; }
        public ICommand EditModuleCommand { get; }
        public ICommand DeleteModuleCommand { get; }

        public ObservableCollection<ModuleViewModel> Modules { get; }

        public Array CourseAccessTypes => Enum.GetValues(typeof(CourseAccessType));

        private async void SaveAsync()
        {
            try
            {
                IsLoading = true;

                if (IsNewCourse)
                {
                    await _courseService.CreateCourseAsync(Course, _currentUser.Id);
                }
                else
                {
                    await _courseService.UpdateCourseAsync(Course, _currentUser.Id);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении курса: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void AddModuleAsync()
        {
            if(_isLoading) return;
            try
            {
                var newModule = new Module
                {
                    Title = "Новый модуль",
                    Description = "Описание модуля",
                    CourseId = Course.Id,
                    OrderIndex = (Course.Modules?.Count ?? 0) + 1
                };

                var dialog = new ModuleEditDialog(newModule);
                if (dialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    if (Course.Id != 0)
                    {
                        newModule.CourseId = Course.Id;
                        var savedModule = await _courseService.AddModuleAsync(Course.Id, newModule, _currentUser.Id);

                        var moduleViewModel = new ModuleViewModel(savedModule);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Modules.Add(moduleViewModel);
                        });
                    }
                    else
                    {
                        newModule.CourseId = 0;

                        var moduleViewModel = new ModuleViewModel(newModule);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Modules.Add(moduleViewModel);
                        });
                        
                        if (Course.Modules == null)
                            Course.Modules = new ObservableCollection<Module>();
                        
                        newModule.CourseId = Course.Id;
                        Course.Modules.Add(newModule);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении модуля: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void EditModuleAsync(ModuleViewModel moduleViewModel)
        {
            try
            {
                var moduleToEdit = new Module
                {
                    Id = moduleViewModel.Id,
                    Title = moduleViewModel.Title,
                    Description = moduleViewModel.Description,
                    OrderIndex = moduleViewModel.OrderIndex,
                    CourseId = moduleViewModel.CourseId
                };
        
                var dialog = new ModuleEditDialog(moduleToEdit);
                if (dialog.ShowDialog() == true)
                {
                    IsLoading = true;
            
                    moduleViewModel.Title = moduleToEdit.Title;
                    moduleViewModel.Description = moduleToEdit.Description;
                    moduleViewModel.OrderIndex = moduleToEdit.OrderIndex;
            
                    if (Course.Id != 0) {
                        // Явно устанавливаем связь с курсом
                        moduleToEdit.CourseId = Course.Id;
                        var updatedModule = await _courseService.UpdateModuleAsync(moduleToEdit, _currentUser.Id);

                        var moduleInCourse = Course.Modules.FirstOrDefault(m => m.Id == updatedModule.Id);
                        if (moduleInCourse != null) {
                            moduleInCourse.Title = updatedModule.Title;
                            moduleInCourse.Description = updatedModule.Description;
                            moduleInCourse.OrderIndex = updatedModule.OrderIndex;
                            moduleInCourse.CourseId = Course.Id; // Явно устанавливаем связь с курсом
                        }
                    } else {
                        // Course doesn't exist in database yet, just update the module in memory
                        var moduleInCourse = Course.Modules.FirstOrDefault(m => 
                            m.OrderIndex == moduleToEdit.OrderIndex || 
                            (m.Id == moduleToEdit.Id && moduleToEdit.Id != 0));
                
                        if (moduleInCourse != null) {
                            moduleInCourse.Title = moduleToEdit.Title;
                            moduleInCourse.Description = moduleToEdit.Description;
                            moduleInCourse.OrderIndex = moduleToEdit.OrderIndex;
                            moduleInCourse.CourseId = Course.Id; // Явно устанавливаем связь с курсом
                        }
                    }

                    OnPropertyChanged(nameof(Modules));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении модуля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void DeleteModuleAsync(ModuleViewModel moduleViewModel)
        {
            try
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить этот модуль?",
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    if (Course.Id != 0)
                    {
                        await _courseService.DeleteModuleAsync(moduleViewModel.Id, _currentUser.Id);
                    }

                    var moduleInCourse = Course.Modules.FirstOrDefault(m => 
                        m.OrderIndex == moduleViewModel.OrderIndex || 
                        m.Id == moduleViewModel.Id || 
                        (moduleViewModel.Id == 0 && m.Title == moduleViewModel.Title));
                    
                    if (moduleInCourse != null)
                    {
                        Course.Modules.Remove(moduleInCourse);
                        // if (Course.Modules is ObservableCollection<Module> obsModules)
                        // {
                        //     obsModules.Remove(moduleInCourse);
                        // }
                        // else
                        // {
                        //     var modulesList = new ObservableCollection<Module>(Course.Modules);
                        //     modulesList.Remove(moduleInCourse);
                        //     Course.Modules = modulesList;
                        // }
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() => { Modules.Remove(moduleViewModel); });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении модуля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanSave()
        {
            return !IsLoading && !HasErrors;
        }
        
        // private void EditQuiz()
        // {
        //     if (SelectedContentType == ContentType.Quiz)
        //     {
        //         Quiz quiz = null;
        //         try
        //         {
        //             quiz = System.Text.Json.JsonSerializer.Deserialize<Quiz>(ContentData);
        //         }
        //         catch
        //         {
        //             quiz = new Quiz();
        //         }
        //
        //         var dialog = new QuizEditDialog(quiz);
        //         if (dialog.ShowDialog() == true)
        //         {
        //             ContentData = System.Text.Json.JsonSerializer.Serialize(quiz);
        //         }
        //     }
        // }

        #region Validation

        public string this[string columnName]
        {
            get
            {
                if (Course == null) return null;

                switch (columnName)
                {
                    case nameof(Course.Title):
                        if (string.IsNullOrWhiteSpace(Course.Title))
                            return "Название курса обязательно для заполнения";
                        if (Course.Title.Length < 3)
                            return "Название курса должно содержать не менее 3 символов";
                        if (Course.Title.Length > 100)
                            return "Название курса не должно превышать 100 символов";
                        return null;

                    case nameof(Course.Description):
                        if (string.IsNullOrWhiteSpace(Course.Description))
                            return "Описание курса обязательно для заполнения";
                        if (Course.Description.Length < 10)
                            return "Описание курса должно содержать не менее 10 символов";
                        if (Course.Description.Length > 2000)
                            return "Описание курса не должно превышать 2000 символов";
                        return null;

                    case nameof(Course.StartDate):
                        if (!Course.StartDate.HasValue)
                            return "Дата начала обязательна для заполнения";
                        if (Course.StartDate < DateTime.Today)
                            return "Дата начала не может быть в прошлом";
                        if (Course.EndDate.HasValue && Course.StartDate > Course.EndDate)
                            return "Дата начала не может быть позже даты окончания";
                        return null;

                    case nameof(Course.EndDate):
                        if (!Course.EndDate.HasValue)
                            return "Дата окончания обязательна для заполнения";
                        if (Course.EndDate < DateTime.Today)
                            return "Дата окончания не может быть в прошлом";
                        if (Course.StartDate.HasValue && Course.EndDate < Course.StartDate)
                            return "Дата окончания не может быть раньше даты начала";
                        return null;

                    // case nameof(Course.MaxStudents):
                    //     if (Course.MaxStudents <= 0)
                    //         return "Максимальное количество студентов должно быть больше 0";
                    //     if (Course.MaxStudents > 1000)
                    //         return "Максимальное количество студентов не может превышать 1000";
                    //     return null;

                    default:
                        return null;
                }
            }
        }

        public string Error => null;

        public bool HasErrors
        {
            get
            {
                if (Course == null) return false;

                return !string.IsNullOrEmpty(this[nameof(Course.Title)]) ||
                       !string.IsNullOrEmpty(this[nameof(Course.Description)]) ||
                       !string.IsNullOrEmpty(this[nameof(Course.StartDate)]) ||
                       !string.IsNullOrEmpty(this[nameof(Course.EndDate)]);
                // !string.IsNullOrEmpty(this[nameof(Course.MaxStudents)]);
            }
        }

        #endregion

        #region Dialog

        public bool? DialogResult { get; set; }

        public void Close()
        {
            var window = Window;
            window.DialogResult = DialogResult;
            window.Close();
        }

        private Window Window => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);

        #endregion
    }
}
