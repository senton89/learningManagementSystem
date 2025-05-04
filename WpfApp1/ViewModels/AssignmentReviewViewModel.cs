using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class AssignmentReviewViewModel : NotifyPropertyChangedBase
    {
        private readonly CourseService _courseService;
        private readonly Assignment _assignment;
        private bool _isLoading;
        private string _searchText;
        private StatusFilter _selectedStatusFilter;
        private AssignmentSubmissionViewModel _selectedSubmission;
        private ObservableCollection<CriteriaScoreViewModel> _criteriaScores;
        private string _teacherComment;
        private int _finalScore;
        private string _penaltyDescription;
        private ObservableCollection<AssignmentSubmissionViewModel> _filteredSubmissions;

        public AssignmentReviewViewModel(CourseService courseService, Assignment assignment)
        {
            _courseService = courseService;
            _assignment = assignment;

            // Collections
            Submissions = new ObservableCollection<AssignmentSubmissionViewModel>();
            FilteredSubmissions = new ObservableCollection<AssignmentSubmissionViewModel>();
            StatusFilters = new ObservableCollection<StatusFilter>
            {
                new StatusFilter { Status = null, Name = "Все" },
                new StatusFilter { Status = SubmissionStatus.Submitted, Name = "Ожидают проверки" },
                new StatusFilter { Status = SubmissionStatus.UnderReview, Name = "В процессе проверки" },
                new StatusFilter { Status = SubmissionStatus.Reviewed, Name = "Проверены" },
                new StatusFilter { Status = SubmissionStatus.RequiresRevision, Name = "Требуют доработки" }
            };
            SelectedStatusFilter = StatusFilters.First();

            // Commands
            OpenFileCommand = new RelayCommand<SubmissionFile>(OpenFile);
            SaveDraftCommand = new RelayCommand(SaveDraftAsync, () => CanSaveDraft);
            RequestRevisionCommand = new RelayCommand(RequestRevisionAsync, () => CanRequestRevision);
            CompleteReviewCommand = new RelayCommand(CompleteReviewAsync, () => CanCompleteReview);

            // Batch operations commands
            DownloadAllSubmissionsCommand = new RelayCommand(DownloadAllSubmissions);
            BatchGradeCommand = new RelayCommand(() => BatchGrade(BatchGradeScore, BatchGradeComment));

            // Load data
            LoadSubmissionsAsync();
        }

        #region Properties

        public ObservableCollection<AssignmentSubmissionViewModel> Submissions { get; }
        
        public ObservableCollection<AssignmentSubmissionViewModel> FilteredSubmissions
        {
            get => _filteredSubmissions;
            private set => SetProperty(ref _filteredSubmissions, value);
        }

        public ObservableCollection<StatusFilter> StatusFilters { get; }

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

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    UpdateFilteredSubmissions();
                }
            }
        }

        public StatusFilter SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    UpdateFilteredSubmissions();
                }
            }
        }

        public AssignmentSubmissionViewModel SelectedSubmission
        {
            get => _selectedSubmission;
            set
            {
                if (SetProperty(ref _selectedSubmission, value))
                {
                    LoadSubmissionDetails();
                    OnPropertyChanged(nameof(HasSelectedSubmission));
                    OnPropertyChanged(nameof(HasFiles));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ObservableCollection<CriteriaScoreViewModel> CriteriaScores
        {
            get => _criteriaScores;
            private set => SetProperty(ref _criteriaScores, value);
        }

        public string TeacherComment
        {
            get => _teacherComment;
            set
            {
                if (SetProperty(ref _teacherComment, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public int FinalScore
        {
            get => _finalScore;
            private set => SetProperty(ref _finalScore, value);
        }

        public string PenaltyDescription
        {
            get => _penaltyDescription;
            private set => SetProperty(ref _penaltyDescription, value);
        }

        public bool HasSelectedSubmission => SelectedSubmission != null;
        public bool HasFiles => SelectedSubmission?.Files.Any() ?? false;
        public bool CanSaveDraft => HasSelectedSubmission && !IsLoading;
        public bool CanRequestRevision => HasSelectedSubmission && !IsLoading && SelectedSubmission.Status != SubmissionStatus.RequiresRevision;
        public bool CanCompleteReview => HasSelectedSubmission && !IsLoading && CriteriaScores?.All(c => c.Score >= 0) == true && !string.IsNullOrWhiteSpace(TeacherComment);

        // Properties for batch grading
        private int _batchGradeScore;
        public int BatchGradeScore
        {
            get => _batchGradeScore;
            set => SetProperty(ref _batchGradeScore, value);
        }

        private string _batchGradeComment;
        public string BatchGradeComment
        {
            get => _batchGradeComment;
            set => SetProperty(ref _batchGradeComment, value);
        }

        #endregion

        #region Commands

        public ICommand OpenFileCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand RequestRevisionCommand { get; }
        public ICommand CompleteReviewCommand { get; }
        public ICommand DownloadAllSubmissionsCommand { get; }
        public ICommand BatchGradeCommand { get; }

        #endregion

        #region Methods

        private async void LoadSubmissionsAsync()
        {
            try
            {
                IsLoading = true;
                Submissions.Clear();
                var submissions = await _courseService.GetSubmissionsAsync(_assignment.Id);
                foreach (var submission in submissions)
                {
                    Submissions.Add(new AssignmentSubmissionViewModel(submission));
                }
                UpdateFilteredSubmissions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ответов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateFilteredSubmissions()
        {
            var query = Submissions.AsEnumerable();

            // Filter by status
            if (SelectedStatusFilter.Status.HasValue)
            {
                query = query.Where(s => s.Status == SelectedStatusFilter.Status.Value);
            }

            // Search by name
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                query = query.Where(s => s.User.FullName.ToLower().Contains(search));
            }

            // Sort: newest first, then by status
            query = query.OrderByDescending(s => s.SubmissionDate)
                         .ThenBy(s => s.Status);

            var filteredList = query.ToList();

            // Update the list while preserving the selected item
            var selectedId = SelectedSubmission?.Id;
            FilteredSubmissions = new ObservableCollection<AssignmentSubmissionViewModel>(filteredList);
            
            if (selectedId.HasValue)
            {
                SelectedSubmission = FilteredSubmissions.FirstOrDefault(s => s.Id == selectedId);
            }
        }

        private void LoadSubmissionDetails()
        {
            if (SelectedSubmission == null)
            {
                CriteriaScores = null;
                TeacherComment = null;
                FinalScore = 0;
                PenaltyDescription = null;
                return;
            }

            // Create criteria scores
            CriteriaScores = new ObservableCollection<CriteriaScoreViewModel>(
                _assignment.Criteria.Select(c =>
                {
                    var existingScore = SelectedSubmission.Submission.CriteriaScores
                        .FirstOrDefault(s => s.CriteriaId == c.Id);

                    var criteriaScoreViewModel = new CriteriaScoreViewModel(c, existingScore);
                    criteriaScoreViewModel.SetScoreChangedCallback((s, propertyName) =>
                    {
                        if (propertyName == nameof(CriteriaScoreViewModel.Score))
                        {
                            UpdateFinalScore();
                        }
                    });

                    return criteriaScoreViewModel;
                }));

            TeacherComment = SelectedSubmission.Submission.TeacherComment;
            UpdateFinalScore();

            // Penalty description
            if (SelectedSubmission.IsLate)
            {
                var daysLate = (int)(SelectedSubmission.SubmissionDate - _assignment.DueDate).TotalDays;
                var penalty = SelectedSubmission.CalculatePenalty();
                PenaltyDescription = $"Задание сдано с опозданием на {daysLate} дней. " +
                                    $"Штраф {penalty}% будет применен к итоговой оценке.";
            }
            else
            {
                PenaltyDescription = null;
            }
        }

        private void UpdateFinalScore()
        {
            if (CriteriaScores == null) return;

            var baseScore = CriteriaScores.Sum(c => c.Score);
            var maxScore = _assignment.Criteria.Sum(c => c.MaxScore);

            // Convert to 100-point scale
            var percentageScore = maxScore > 0 ? (int)Math.Round((double)baseScore / maxScore * 100) : 0;

            // Apply late penalty
            if (SelectedSubmission.IsLate)
            {
                var penalty = SelectedSubmission.CalculatePenalty();
                percentageScore = Math.Max(0, percentageScore - penalty);
            }

            FinalScore = percentageScore;
        }

        private void OpenFile(SubmissionFile file)
        {
            try
            {
                if (File.Exists(file.FilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = file.FilePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Файл не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveDraftAsync()
        {
            try
            {
                IsLoading = true;
                await SaveReviewAsync(SubmissionStatus.UnderReview);
                MessageBox.Show("Черновик проверки сохранен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении черновика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void RequestRevisionAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TeacherComment))
                {
                    MessageBox.Show("Необходимо добавить комментарий с описанием требуемых изменений", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    "Вы уверены, что хотите запросить доработку задания?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    await SaveReviewAsync(SubmissionStatus.RequiresRevision);
                    MessageBox.Show("Запрос на доработку отправлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запросе доработки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void CompleteReviewAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите завершить проверку? После этого изменение оценки будет невозможно.",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    await SaveReviewAsync(SubmissionStatus.Reviewed);
                    MessageBox.Show("Проверка завершена", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при завершении проверки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveReviewAsync(SubmissionStatus status)
        {
            var submission = SelectedSubmission.Submission;
            submission.Status = status;
            submission.TeacherComment = TeacherComment;
            submission.Score = FinalScore;
            submission.ReviewDate = DateTime.UtcNow;

            submission.CriteriaScores.Clear();
            foreach (var score in CriteriaScores)
            {
                submission.CriteriaScores.Add(new CriteriaScore
                {
                    CriteriaId = score.Criteria.Id,
                    Score = score.Score,
                    Comment = score.Comment
                });
            }

            await _courseService.SaveSubmissionAsync(submission);
            SelectedSubmission.Update(submission);
            LoadSubmissionsAsync();
        }

        // Enhanced teacher functionality for batch operations
        public void DownloadAllSubmissions()
        {
            try
            {
                if (Submissions.Count == 0) return;

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Select Folder to Save Submissions",
                    FileName = "Select Folder", // This is a placeholder
                    CheckPathExists = true,
                    ValidateNames = false,
                    CheckFileExists = false,
                    Filter = "Folder|*.this.directory", // Trick to enable folder selection
                    OverwritePrompt = false
                };
                
                if (dialog.ShowDialog() == true)
                {
                    string targetFolder = Path.GetDirectoryName(dialog.FileName);
                    string assignmentFolder = Path.Combine(targetFolder,
                        $"Assignment_{_assignment.Id}_{_assignment.Title.Replace(" ", "_")}");

                    Directory.CreateDirectory(assignmentFolder);

                    foreach (var submission in Submissions)
                    {
                        string studentFolder = Path.Combine(assignmentFolder,
                            $"{submission.User.Id}_{submission.User.FullName.Replace(" ", "_")}");
                        Directory.CreateDirectory(studentFolder);

                        // Save text answer if exists
                        if (!string.IsNullOrWhiteSpace(submission.Submission.TextAnswer))
                        {
                            File.WriteAllText(
                                Path.Combine(studentFolder, "answer.txt"),
                                submission.Submission.TextAnswer);
                        }

                        // Copy files
                        foreach (var file in submission.Files)
                        {
                            if (File.Exists(file.FilePath))
                            {
                                File.Copy(file.FilePath, Path.Combine(studentFolder, file.FileName), true);
                            }
                        }
                    }

                    MessageBox.Show($"All submissions downloaded to {assignmentFolder}",
                        "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading submissions: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void BatchGrade(int score, string comment)
        {
            try
            {
                // Check if there are any selected submissions
                var selectedSubmissions = Submissions.Where(s => s.IsSelected).ToList();
                if (selectedSubmissions.Count == 0)
                {
                    MessageBox.Show("Please select at least one submission to grade", 
                        "No Submissions Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to assign a score of {score} to {selectedSubmissions.Count} selected submissions?",
                    "Confirm Batch Grading",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    
                    // Process each selected submission
                    foreach (var submission in selectedSubmissions)
                    {
                        submission.Submission.Score = score;
                        submission.Submission.TeacherComment = comment;
                        submission.Submission.Status = SubmissionStatus.Reviewed;
                        submission.Submission.ReviewDate = DateTime.UtcNow;

                        _courseService.SaveSubmissionAsync(submission.Submission);
                    }

                    MessageBox.Show("Batch grading completed successfully", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Reload submissions to reflect changes
                    LoadSubmissionsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during batch grading: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }

    public class StatusFilter
    {
        public SubmissionStatus? Status { get; set; }
        public string Name { get; set; }
    }

    public class AssignmentSubmissionViewModel : NotifyPropertyChangedBase
    {
        private AssignmentSubmission _submission;
        private bool _isSelected;

        public AssignmentSubmissionViewModel(AssignmentSubmission submission)
        {
            _submission = submission;
        }

        public void Update(AssignmentSubmission submission)
        {
            _submission = submission;
            OnPropertyChanged(string.Empty);
        }

        public int Id => _submission.Id;
        public User User => _submission.User;
        public DateTime SubmissionDate => _submission.SubmissionDate;
        public SubmissionStatus Status => _submission.Status;
        public AssignmentSubmission Submission => _submission;
        public IEnumerable<SubmissionFile> Files => _submission.Files;
        public bool IsLate => _submission.IsLate;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int CalculatePenalty() => _submission.CalculatePenalty();

        public Brush StatusBrush
        {
            get
            {
                return Status switch
                {
                    SubmissionStatus.Draft => Brushes.Gray,
                    SubmissionStatus.Submitted => Brushes.Orange,
                    SubmissionStatus.UnderReview => Brushes.Blue,
                    SubmissionStatus.Reviewed => Brushes.Green,
                    SubmissionStatus.RequiresRevision => Brushes.Red,
                    _ => Brushes.Black
                };
            }
        }
    }

    public class CriteriaScoreViewModel : NotifyPropertyChangedBase
    {
        private readonly AssignmentCriteria _criteria;
        private int _score;
        private string _comment;

        private Action<CriteriaScoreViewModel, string> _scoreChangedCallback;

        public CriteriaScoreViewModel(AssignmentCriteria criteria, CriteriaScore existingScore = null)
        {
            _criteria = criteria;
            _score = existingScore?.Score ?? -1;
            _comment = existingScore?.Comment;
        }

        public AssignmentCriteria Criteria => _criteria;

        public int Score
        {
            get => _score;
            set
            {
                if (value >= -1 && value <= Criteria.MaxScore)
                {
                    if (SetProperty(ref _score, value))
                    {
                        _scoreChangedCallback?.Invoke(this, nameof(Score));
                    }
                }
            }
        }

        public string Comment
        {
            get => _comment;
            set => SetProperty(ref _comment, value);
        }

        public void SetScoreChangedCallback(Action<CriteriaScoreViewModel, string> callback)
        {
            _scoreChangedCallback = callback;
        }
    }
}