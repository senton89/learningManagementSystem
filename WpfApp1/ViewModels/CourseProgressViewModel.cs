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
    // CourseProgressViewModel.cs - исправление
    public class CourseProgressViewModel : NotifyPropertyChangedBase
    {
        private readonly CourseService _courseService;
        private readonly User _currentUser;
        private bool _isLoading;
        private Course _course;
        private double _totalProgress;
        private string _progressSummary;

        public ObservableCollection<ModuleProgressViewModel> ModuleProgress { get; } = new();

        public Course Course
        {
            get => _course;
            private set => SetProperty(ref _course, value);
        }

        public double TotalProgress
        {
            get => _totalProgress;
            private set => SetProperty(ref _totalProgress, value);
        }

        public string ProgressSummary
        {
            get => _progressSummary;
            private set => SetProperty(ref _progressSummary, value);
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

        public ICommand StartContentCommand { get; }

        public CourseProgressViewModel(CourseService courseService, User currentUser, Course course)
        {
            _courseService = courseService;
            _currentUser = currentUser;
            _course = course;

            StartContentCommand = new RelayCommand<ContentProgressViewModel>(StartContentAsync);

            LoadProgressAsync();
        }

        private async void LoadProgressAsync()
        {
            try
            {
                IsLoading = true;
                ModuleProgress.Clear();

                var progress = await _courseService.GetStudentProgressAsync(Course.Id, _currentUser.Id);
                var moduleGroups = progress.GroupBy(p => p.Content.Module);

                foreach (var moduleGroup in moduleGroups.OrderBy(g => g.Key.OrderIndex))
                {
                    var moduleProgress = new ModuleProgressViewModel(moduleGroup.Key);

                    foreach (var contentProgress in moduleGroup.OrderBy(p => p.Content.OrderIndex))
                    {
                        moduleProgress.ContentProgress.Add(new ContentProgressViewModel(contentProgress));
                    }

                    ModuleProgress.Add(moduleProgress);
                }

                UpdateTotalProgress();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке прогресса: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void StartContentAsync(ContentProgressViewModel content)
        {
            try
            {
                IsLoading = true;

                // В зависимости от типа контента открываем соответствующее окно
                switch (content.Content.Type)
                {
                    case ContentType.Text:
                        var textDialog = new TextContentDialog();
                        textDialog.DataContext = new TextContentViewModel(content.Content);
                        if (textDialog.ShowDialog() == true)
                        {
                            await UpdateProgressAsync(content, ProgressStatus.Completed);
                        }

                        break;

                    case ContentType.Video:
                        var videoDialog = new VideoContentDialog();
                        videoDialog.Loaded += (s, e) =>
                        {
                            var mediaElement = videoDialog.FindName("VideoPlayer") as MediaElement;
                            videoDialog.DataContext = new VideoContentViewModel(content.Content, mediaElement);
                        };
                        if (videoDialog.ShowDialog() == true)
                        {
                            await UpdateProgressAsync(content, ProgressStatus.Completed);
                        }

                        break;

                    case ContentType.Quiz:
                        var quiz = await _courseService.GetQuizAsync(content.Content.Id);
                        var quizDialog = new QuizDialog(quiz);
                        quizDialog.DataContext = new QuizViewModel(_courseService, _currentUser, content.Content);
                        if (quizDialog.ShowDialog() == true)
                        {
                            // Get score from the ViewModel
                            var quizViewModel = quizDialog.DataContext as QuizViewModel;
                            int score = quizViewModel?.Score ?? 0;
                            await UpdateProgressAsync(content, ProgressStatus.Completed, score);
                        }

                        break;

                    case ContentType.Assignment:
                        var assignmentDialog = new AssignmentDialog();
                        assignmentDialog.DataContext = new AssignmentViewModel(
                            await _courseService.GetAssignmentAsync(content.Content.Id),
                            _courseService,
                            _courseService.UserService);
                        if (assignmentDialog.ShowDialog() == true)
                        {
                            await UpdateProgressAsync(content, ProgressStatus.Completed);
                        }

                        break;
                }

                LoadProgressAsync(); // Перезагружаем прогресс
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске контента: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateProgressAsync(ContentProgressViewModel content, ProgressStatus status,
            int? score = null)
        {
            var progress = new StudentProgress
            {
                UserId = _currentUser.Id,
                ContentId = content.Content.Id,
                Status = status,
                Score = score,
                CompletionDate = status == ProgressStatus.Completed ? DateTime.UtcNow : null
            };

            await _courseService.UpdateProgressAsync(progress);
        }

        private void UpdateTotalProgress()
        {
            var totalContent = ModuleProgress.Sum(m => m.ContentProgress.Count);
            var completedContent =
                ModuleProgress.Sum(m => m.ContentProgress.Count(c => c.Status == ProgressStatus.Completed));

            TotalProgress = totalContent > 0 ? (double)completedContent / totalContent * 100 : 0;

            var requiredContent = ModuleProgress.Sum(m => m.ContentProgress.Count(c => c.Content.OrderIndex > 0));
            var completedRequired = ModuleProgress.Sum(m =>
                m.ContentProgress.Count(c => c.Content.OrderIndex > 0 && c.Status == ProgressStatus.Completed));

            ProgressSummary = $"Выполнено {completedContent} из {totalContent} элементов курса" +
                              $" (обязательных: {completedRequired}/{requiredContent})";
        }
    }
}
