using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class QuizEditViewModel : NotifyPropertyChangedBase
    {
        private Quiz _quiz;
        private string _title;
        private string _description;
        private int _timeLimitMinutes;
        private int _passingScore;
        private bool _randomizeQuestions;
        private bool _showCorrectAnswers;
        private Question _selectedQuestion;
        private string _correctAnswer;

        public event EventHandler RequestClose;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public int TimeLimitMinutes
        {
            get => _timeLimitMinutes;
            set => SetProperty(ref _timeLimitMinutes, value);
        }

        public int PassingScore
        {
            get => _passingScore;
            set => SetProperty(ref _passingScore, value);
        }

        public bool RandomizeQuestions
        {
            get => _randomizeQuestions;
            set => SetProperty(ref _randomizeQuestions, value);
        }

        public bool ShowCorrectAnswers
        {
            get => _showCorrectAnswers;
            set => SetProperty(ref _showCorrectAnswers, value);
        }

        public ObservableCollection<Question> Questions { get; }

        public Question SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                if (SetProperty(ref _selectedQuestion, value))
                {
                    OnPropertyChanged(nameof(IsQuestionSelected));
                    OnPropertyChanged(nameof(IsChoiceQuestion));
                    OnPropertyChanged(nameof(IsTextQuestion));
                    OnPropertyChanged(nameof(CanAddAnswers));
                }
            }
        }

        public string CorrectAnswer
        {
            get => _correctAnswer;
            set => SetProperty(ref _correctAnswer, value);
        }

        public bool IsQuestionSelected => SelectedQuestion != null;
        
        public bool IsChoiceQuestion => SelectedQuestion != null && 
                                       (SelectedQuestion.Type == QuestionType.SingleChoice || 
                                        SelectedQuestion.Type == QuestionType.MultipleChoice || 
                                        SelectedQuestion.Type == QuestionType.TrueFalse);
        
        public bool IsTextQuestion => SelectedQuestion != null && 
                                     (SelectedQuestion.Type == QuestionType.ShortAnswer || 
                                      SelectedQuestion.Type == QuestionType.Essay);
        
        public bool CanAddAnswers => IsChoiceQuestion;
        
        private readonly CourseService _courseService;
        private readonly Content _content;

        public IEnumerable<QuestionType> QuestionTypes => Enum.GetValues(typeof(QuestionType)).Cast<QuestionType>();

        public ICommand AddQuestionCommand { get; }
        public ICommand EditQuestionCommand { get; }
        public ICommand DeleteQuestionCommand { get; }
        public ICommand AddAnswerCommand { get; }
        public ICommand DeleteAnswerCommand { get; }
        public ICommand SaveCommand { get; }

        public QuizEditViewModel(Quiz quiz, CourseService courseService = null, Content content = null)
        {
            _quiz = quiz ?? new Quiz();
            _courseService = courseService;
            _content = content;
            _title = quiz?.Title ?? string.Empty;
            _description = quiz?.Description ?? string.Empty;
            _timeLimitMinutes = quiz?.TimeLimit.TotalMinutes > 0 ? (int)quiz.TimeLimit.TotalMinutes : 30;
            _passingScore = quiz?.PassingScore ?? 70;
            _randomizeQuestions = quiz?.RandomizeQuestions ?? false;
            _showCorrectAnswers = quiz?.ShowCorrectAnswers ?? true;

            Questions = new ObservableCollection<Question>(quiz?.Questions ?? new List<Question>());

            AddQuestionCommand = new RelayCommand(AddQuestion);
            EditQuestionCommand = new RelayCommand<Question>(EditQuestion);
            DeleteQuestionCommand = new RelayCommand<Question>(DeleteQuestion);
            AddAnswerCommand = new RelayCommand(AddAnswer, () => CanAddAnswers);
            DeleteAnswerCommand = new RelayCommand<Answer>(DeleteAnswer);
            SaveCommand = new RelayCommand(Save, CanSave);
        }

        private void AddQuestion()
        {
            var question = new Question
            {
                Text = "New Question",
                Type = QuestionType.SingleChoice,
                Points = 1,
                Answers = new List<Answer>()
            };
            
            Questions.Add(question);
            SelectedQuestion = question;
        }

        private void EditQuestion(Question question)
        {
            SelectedQuestion = question;
        }

        private void DeleteQuestion(Question question)
        {
            if (question != null)
            {
                Questions.Remove(question);
                if (SelectedQuestion == question)
                {
                    SelectedQuestion = Questions.FirstOrDefault();
                }
            }
        }

        private void AddAnswer()
        {
            if (SelectedQuestion != null && IsChoiceQuestion)
            {
                var answer = new Answer
                {
                    Text = "New Answer",
                    IsCorrect = false,
                    QuestionId = SelectedQuestion.Id
                };
                
                if (SelectedQuestion.Answers == null)
                {
                    SelectedQuestion.Answers = new List<Answer>();
                }
                
                SelectedQuestion.Answers.Add(answer);
                
                // Force UI update
                var temp = SelectedQuestion;
                SelectedQuestion = null;
                SelectedQuestion = temp;
            }
        }

        private void DeleteAnswer(Answer answer)
        {
            if (SelectedQuestion != null && answer != null)
            {
                SelectedQuestion.Answers.Remove(answer);
                
                // Force UI update
                var temp = SelectedQuestion;
                SelectedQuestion = null;
                SelectedQuestion = temp;
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title) && Questions.Count > 0;
        }

        private async void Save()
        {
            try
            {
                _quiz.Title = Title;
                _quiz.Description = Description;
                _quiz.TimeLimit = TimeSpan.FromMinutes(TimeLimitMinutes);
                _quiz.PassingScore = PassingScore;
                _quiz.RandomizeQuestions = RandomizeQuestions;
                _quiz.ShowCorrectAnswers = ShowCorrectAnswers;
        
                // Clear existing questions and add the current ones
                _quiz.Questions.Clear();
                foreach (var question in Questions)
                {
                    _quiz.Questions.Add(question);
                }
                
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving quiz: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
