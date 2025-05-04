using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class QuestionViewModel : NotifyPropertyChangedBase
    {
        private readonly Question _question;
        private string _textResponse;
        private bool _isCorrect;
        private int _pointsEarned;

        public Question Question => _question;
        public ObservableCollection<AnswerViewModel> Answers { get; } = new();
        
        public string TextResponse
        {
            get => _textResponse;
            set => SetProperty(ref _textResponse, value);
        }

        public bool IsCorrect
        {
            get => _isCorrect;
            set => SetProperty(ref _isCorrect, value);
        }

        public int PointsEarned
        {
            get => _pointsEarned;
            set => SetProperty(ref _pointsEarned, value);
        }

        public bool IsAnswered
        {
            get
            {
                if (IsSingleChoice)
                    return Answers.Any(a => a.IsSelected);
                if (IsMultipleChoice)
                    return true;
                return !string.IsNullOrWhiteSpace(TextResponse);
            }
        }

        public bool IsSingleChoice => Question.Type == QuestionType.SingleChoice || Question.Type == QuestionType.TrueFalse;
        public bool IsMultipleChoice => Question.Type == QuestionType.MultipleChoice;
        public bool IsTextResponse => Question.Type == QuestionType.ShortAnswer || Question.Type == QuestionType.Essay;
        public bool IsEssay => Question.Type == QuestionType.Essay;

        public QuestionViewModel(Question question)
        {
            _question = question;
    
            foreach (var answer in question.Answers) 
            { 
                var answerVM = new AnswerViewModel(answer);
                answerVM.PropertyChanged += (s, e) => 
                {
                    if (e.PropertyName == nameof(AnswerViewModel.IsSelected))
                    {
                        // Уведомляем QuizViewModel об изменении
                        OnPropertyChanged(nameof(IsAnswered));
                    }
                };
                Answers.Add(answerVM); 
            }
        }

        public string UserResponse
        {
            get
            {
                if (IsSingleChoice || IsMultipleChoice)
                {
                    return string.Join(", ", Answers.Where(a => a.IsSelected).Select(a => a.Text));
                }
                return TextResponse;
            }
        }

        public string CorrectAnswer
        {
            get
            {
                if (IsSingleChoice || IsMultipleChoice)
                {
                    return string.Join(", ", Question.Answers.Where(a => a.IsCorrect).Select(a => a.Text));
                }
                return ""; // For text responses, there's no predefined correct answer
            }
        }
        public void NotifyAnswerChanged()
        {
            OnPropertyChanged(nameof(IsAnswered));
        }
        
        public void UpdateSelectedAnswers(IEnumerable<AnswerViewModel> selectedAnswers)
        {
            foreach (var answer in Answers)
            {
                answer.IsSelected = selectedAnswers.Where(a => a.IsSelected).Select(a => a.Text).Contains(answer.Text);
            }
            OnPropertyChanged(nameof(UserResponse));
        }
    }

    public class AnswerViewModel : NotifyPropertyChangedBase
    {
        private readonly Answer _answer;
        private bool _isSelected;

        public int Id => _answer.Id;
        public string Text => _answer.Text;
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    // Уведомляем родительский QuestionViewModel об изменении
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public AnswerViewModel(Answer answer)
        {
            _answer = answer;
        }
    }
}