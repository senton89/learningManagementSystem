using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class QuizViewModel : NotifyPropertyChangedBase
    {
        private readonly CourseService _courseService;
        private readonly User _currentUser;
        private readonly DispatcherTimer _timer;

        private bool _isLoading;
        private bool _isQuizInProgress;
        private bool _isQuizCompleted;
        private Quiz _quiz;
        private QuestionViewModel _currentQuestion;
        private int _currentQuestionIndex;
        private TimeSpan _remainingTime;
        private double _progress;
        private int _score;
        private bool _isPassed;

        public ObservableCollection<QuestionViewModel> Questions { get; } = new();

        public Quiz Quiz
        {
            get => _quiz;
            private set => SetProperty(ref _quiz, value);
        }

        public QuestionViewModel CurrentQuestion
        {
            get => _currentQuestion;
            private set => SetProperty(ref _currentQuestion, value);
        }

        public int CurrentQuestionIndex
        {
            get => _currentQuestionIndex;
            private set
            {
                if (SetProperty(ref _currentQuestionIndex, value))
                {
                    OnPropertyChanged(nameof(CanGoToPrevious));
                    OnPropertyChanged(nameof(CanGoToNext));
                    OnPropertyChanged(nameof(NextButtonText));
                    OnPropertyChanged(nameof(DisplayQuestionIndex));
                    UpdateProgress();
                }
            }
        }

        public int DisplayQuestionIndex => CurrentQuestionIndex + 1;
        public int TotalQuestions => Questions.Count;

        public TimeSpan RemainingTime
        {
            get => _remainingTime;
            private set => SetProperty(ref _remainingTime, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
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

        public bool IsQuizInProgress
        {
            get => _isQuizInProgress;
            private set => SetProperty(ref _isQuizInProgress, value);
        }

        public bool IsQuizCompleted
        {
            get => _isQuizCompleted;
            private set => SetProperty(ref _isQuizCompleted, value);
        }

        public int Score
        {
            get => _score;
            private set => SetProperty(ref _score, value);
        }

        public bool IsPassed
        {
            get => _isPassed;
            private set => SetProperty(ref _isPassed, value);
        }
        
        private Content content { get; }

        public bool CanGoToPrevious => CurrentQuestionIndex > 0;

        public bool CanGoToNext => CurrentQuestion?.IsAnswered ?? false;

        public string NextButtonText => CurrentQuestionIndex == TotalQuestions - 1 ? "Завершить" : "Следующий";

        public ICommand PreviousQuestionCommand { get; }
        public ICommand NextQuestionCommand { get; }

        public QuizViewModel(CourseService courseService, User currentUser, Content content)
        {
            _courseService = courseService;
            _currentUser = currentUser;
            this.content = content;

            // Настраиваем таймер
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            // Команды
            PreviousQuestionCommand = new RelayCommand(PreviousQuestion, () => CanGoToPrevious);
            NextQuestionCommand = new RelayCommand(NextQuestion, () => CanGoToNext);
            
            // Загружаем тест
            LoadQuizAsync(content);
        }

        private void UpdateCommandStates()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        // Обновить метод установки текущего вопроса
        private void SetCurrentQuestion(int index)
        {
            CurrentQuestionIndex = index;
            CurrentQuestion = Questions[CurrentQuestionIndex];
        
            // Подписываемся на изменения в ответах текущего вопроса
            foreach (var answer in CurrentQuestion.Answers)
            {
                answer.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(AnswerViewModel.IsSelected))
                    {
                        OnPropertyChanged(nameof(CanGoToNext));
                        UpdateCommandStates();
                    }
                };
            }
        
            // Подписываемся на изменения в текстовом ответе
            CurrentQuestion.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(QuestionViewModel.TextResponse) || 
                    e.PropertyName == nameof(QuestionViewModel.IsAnswered))
                {
                    OnPropertyChanged(nameof(CanGoToNext));
                    UpdateCommandStates();
                }
            };
        }
        
        private async void LoadQuizAsync(Content content)
        {
            try
            {
                IsLoading = true;
                Questions.Clear();

                // Загружаем тест из контента
                Quiz = await _courseService.GetQuizAsync(content.Id);

                if (Quiz.Questions == null || !Quiz.Questions.Any())
                {
                    MessageBox.Show("Тест не содержит вопросов", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DialogResult = false;
                    return;
                }
                
                // Создаем вопросы
                var questions = Quiz.RandomizeQuestions
                    ? Quiz.Questions.OrderBy(q => Guid.NewGuid())
                    : Quiz.Questions.OrderBy(q => q.Id);

                foreach (var question in questions)
                {
                    Questions.Add(new QuestionViewModel(question));
                }
                
                OnPropertyChanged(nameof(TotalQuestions));
                
                if (Questions.Count > 0)
                {
                    SetCurrentQuestion(0);
                }
                
                // Запускаем таймер
                RemainingTime = Quiz.TimeLimit;
                _timer.Start();

                IsQuizInProgress = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке теста: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (RemainingTime > TimeSpan.Zero)
            {
                RemainingTime = RemainingTime.Subtract(TimeSpan.FromSeconds(1));
            }
            else
            {
                CompleteQuiz();
            }
        }

        private void PreviousQuestion()
        {
            if (CanGoToPrevious)
            {
                SetCurrentQuestion(CurrentQuestionIndex - 1);
            }
        }

        private void NextQuestion()
        {
            if (CurrentQuestionIndex < TotalQuestions - 1)
            {
                SetCurrentQuestion(CurrentQuestionIndex + 1);
            }
            else
            {
                CompleteQuiz();
            }
        }

        private void UpdateProgress()
        {
            Progress = (double)(CurrentQuestionIndex + 1) / TotalQuestions * 100;
        }

        private async void CompleteQuiz()
        {
            try
            {
                IsLoading = true;
                _timer.Stop();

                // Вычисляем результаты для каждого вопроса
                foreach (var question in Questions)
                {
                    // Для вопросов с выбором
                    if (question.IsSingleChoice || question.IsMultipleChoice)
                    {
                        bool isCorrect = true;
                        var selectedAnswerIds = question.Answers
                            .Where(a => a.IsSelected)
                            .ToList();
                
                        // Обновляем выбранные ответы в модели вопроса
                        question.UpdateSelectedAnswers(selectedAnswerIds);
                        foreach (var answer in question.Question.Answers)
                        {
                            var selectedAnswer = question.Answers.FirstOrDefault(a => a.Text == answer.Text);
                            if ((selectedAnswer?.IsSelected != answer.IsCorrect))
                            {
                                isCorrect = false;
                                break;
                            }
                        }

                        question.IsCorrect = isCorrect;
                        question.PointsEarned = isCorrect ? question.Question.Points : 0;
                    }
                    // Для текстовых вопросов (упрощенная проверка)
                    else if (question.IsTextResponse && !string.IsNullOrEmpty(question.TextResponse))
                    {
                        // Для простоты считаем любой непустой ответ правильным
                        question.IsCorrect = true;
                        question.PointsEarned = question.Question.Points;
                    }
                }

                // Создаем попытку
                var attempt = new QuizAttempt
                {
                    QuizId = Quiz.Id,
                    Quiz = Quiz,
                    UserId = _currentUser.Id,
                    StartTime = DateTime.UtcNow.Subtract(Quiz.TimeLimit).Add(RemainingTime),
                    EndTime = DateTime.UtcNow,
                    Responses = new List<QuestionResponse>()
                };

                // Добавляем ответы
                foreach (var q in Questions)
                {
                    var response = new QuestionResponse
                    {
                        QuestionId = q.Question.Id,
                        SelectedAnswerIds = q.Answers.Where(a => a.IsSelected).Select(a => a.Id).ToList(),
                        TextResponse = q.TextResponse,
                        IsCorrect = q.IsCorrect,
                        PointsEarned = q.PointsEarned
                    };
                    
                    // Добавляем выбранные ответы для вопросов с выбором
                    if (q.IsSingleChoice || q.IsMultipleChoice)
                    {
                        response.SelectedAnswerIds = q.Answers
                            .Where(a => a.IsSelected)
                            .Select(a => a.Id)
                            .ToList();
                    }
                    attempt.Responses.Add(response);
                }

                // Рассчитываем общий балл
                var totalPoints = Questions.Sum(q => q.Question.Points);
                var earnedPoints = Questions.Sum(q => q.PointsEarned);
        
                // Преобразуем в 100-балльную шкалу
                int percentageScore = totalPoints > 0 
                    ? (int)Math.Round((double)earnedPoints / totalPoints * 100) 
                    : 0;
        
                attempt.Score = percentageScore;
                attempt.IsPassed = percentageScore >= Quiz.PassingScore;
                
                // Сохраняем результаты
                attempt = await _courseService.SaveQuizAttemptAsync(attempt);

                // Обновляем UI
                Score = attempt.Score;
                IsPassed = attempt.IsPassed;
                
                IsQuizInProgress = false;
                IsQuizCompleted = true;

                // Обновляем прогресс студента
                await UpdateStudentProgress();

                // Закрываем диалог с результатом
                DialogResult = true;

                await Task.Delay(5000);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении результатов: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        public void Close()
        {
            var window = Window;
            window.DialogResult = DialogResult;
            window.Close();
        }

        private Window Window => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);

        private async Task UpdateStudentProgress()
        {
            try
            {
                // Создаем запись о прогрессе
                var progress = new StudentProgress
                {
                    UserId = _currentUser.Id,
                    ContentId = content.Id,
                    Status = ProgressStatus.Completed,
                    Score = Score,
                    CompletionDate = DateTime.UtcNow
                };

                // Сохраняем прогресс
                await _courseService.UpdateProgressAsync(progress);
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем выполнение
                Console.WriteLine($"Error updating student progress: {ex.Message}");
            }
        }

        public bool? DialogResult { get; set; }

        public void Cleanup()
        {
            _timer.Stop();
        }
    }
}