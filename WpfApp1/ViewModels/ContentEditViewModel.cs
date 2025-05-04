using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Views;

namespace WpfApp1.ViewModels
{
    public class ContentEditViewModel : NotifyPropertyChangedBase
    {
        private readonly Content _content;
        private readonly int _moduleId;
        private string _title;
        private ContentType _selectedContentType;
        private string _contentData;
        private int _orderIndex;

        public event EventHandler RequestClose;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public IEnumerable<ContentType> ContentTypes => Enum.GetValues(typeof(ContentType)).Cast<ContentType>();

        public ContentType SelectedContentType
        {
            get => _selectedContentType;
            set
            {
                if (SetProperty(ref _selectedContentType, value))
                {
                    OnPropertyChanged(nameof(IsTextContent));
                    OnPropertyChanged(nameof(IsVideoContent));
                    OnPropertyChanged(nameof(IsQuizContent));
                    OnPropertyChanged(nameof(IsAssignmentContent));
                }
            }
        }

        public string ContentData
        {
            get => _contentData;
            set => SetProperty(ref _contentData, value);
        }

        public int OrderIndex
        {
            get => _orderIndex;
            set => SetProperty(ref _orderIndex, value);
        }
        
        private CourseService _courseService;

        public bool IsTextContent => SelectedContentType == ContentType.Text;
        public bool IsVideoContent => SelectedContentType == ContentType.Video;
        public bool IsQuizContent => SelectedContentType == ContentType.Quiz;
        public bool IsAssignmentContent => SelectedContentType == ContentType.Assignment;

        public ICommand SaveCommand { get; }
        public ICommand EditQuizCommand { get; }

        public ContentEditViewModel(Content content, int moduleId, CourseService courseService = null)
        {
            _content = content ?? new Content();
            _moduleId = moduleId;
            _title = content?.Title ?? string.Empty;
            _selectedContentType = content?.Type ?? ContentType.Text;
            _contentData = content?.Data ?? string.Empty;
            _orderIndex = content?.OrderIndex ?? 0;
            _courseService = courseService;

            SaveCommand = new RelayCommand(Save, CanSave);
            EditQuizCommand = new RelayCommand(EditQuiz, () => IsQuizContent);
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title);
        }

        private void Save()
        {
            _content.Title = Title;
            _content.Type = SelectedContentType;
            _content.Data = ContentData;
            _content.OrderIndex = OrderIndex;
            _content.ModuleId = _moduleId;

            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        
        private async void EditQuiz()
        {
            if (SelectedContentType == ContentType.Quiz)
            {
                Quiz quiz = null;
                try
                {
                    quiz = System.Text.Json.JsonSerializer.Deserialize<Quiz>(ContentData);
                }
                catch
                {
                    quiz = new Quiz();
                }

                var dialog = new QuizEditDialog(quiz, _courseService, _content);
                if (dialog.ShowDialog() == true)
                {
                    // Сохраняем Quiz в базу данных, если это новый Quiz
                    if (quiz.Id == 0 && _content.Id != 0)
                    {
                        // Устанавливаем связь с контентом
                        quiz.ContentId = _content.Id;
                
                        // Сохраняем Quiz в базу данных через сервис
                        var savedQuiz = await _courseService.SaveQuizAsync(quiz, quiz.ContentId);
                
                        // Обновляем ID после сохранения
                        quiz = savedQuiz;
                    }
            
                    // Сериализуем обновленный Quiz с правильным ID
                    ContentData = System.Text.Json.JsonSerializer.Serialize(quiz);
                }
            }
        }
    }
}
