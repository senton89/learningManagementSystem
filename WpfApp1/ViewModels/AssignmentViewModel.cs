using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Windows.Storage.Pickers.Provider;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WpfApp1.Controls;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public partial class AssignmentViewModel : ViewModelBase
    {
        private readonly Assignment _assignment;
        private readonly CourseService _courseService;
        private readonly UserService _userService;
        private string _textAnswer;
        private SubmissionStatus _status;

        public string Title => _assignment.Title;
        public string Description => _assignment.Description;
        public DateTime DueDate => _assignment.DueDate;
        public int MaxScore => _assignment.MaxScore;

        public string TextAnswer
        {
            get => _textAnswer;
            set => SetProperty(ref _textAnswer, value);
        }

        public SubmissionStatus Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public ObservableCollection<FileInfo> Files { get; } = new();
        
        // Свойства для работы с критериями
        public ObservableCollection<AssignmentCriteria> Criteria { get; } = new();
        private string _newCriteriaDescription;
        public string NewCriteriaDescription
        {
            get => _newCriteriaDescription;
            set
            {
                if (SetProperty(ref _newCriteriaDescription, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private int _newCriteriaMaxScore;
        public int NewCriteriaMaxScore
        {
            get => _newCriteriaMaxScore;
            set
            {
                if (SetProperty(ref _newCriteriaMaxScore, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private AssignmentCriteria _selectedCriteria;
        public AssignmentCriteria SelectedCriteria
        {
            get => _selectedCriteria;
            set => SetProperty(ref _selectedCriteria, value);
        }
        
        public string AllowedFileExtensionsText
        {
            get => string.Join(", ", _assignment.AllowedFileExtensions);
            set
            {
                var extensions = value.Split(',')
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
        
                _assignment.AllowedFileExtensions = extensions;
                OnPropertyChanged();
            }
        }
        
        public bool? DialogResult { get; set; }
        
        private Window Window => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);

// Команды для работы с критериями
        public ICommand AddCriteriaCommand { get; }
        public ICommand RemoveCriteriaCommand { get; }

        public ICommand SubmitCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand AddFileCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand SaveCommand { get; }

        public AssignmentViewModel(Assignment assignment, CourseService courseService, UserService userService)
        {
            _assignment = assignment;
            _courseService = courseService;
            _userService = userService;

            // Инициализируем коллекцию критериев
            if (_assignment.Criteria == null) {
                _assignment.Criteria = new List<AssignmentCriteria>();
            }
    
            Criteria = new ObservableCollection<AssignmentCriteria>(_assignment.Criteria);


            // Команды для работы с заданием
            SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
            SaveDraftCommand = new AsyncRelayCommand(SaveDraftAsync);
            AddFileCommand = new RelayCommand(AddFile);
            RemoveFileCommand = new RelayCommand<FileInfo>(RemoveFile);

            // Команды для работы с критериями
            AddCriteriaCommand = new RelayCommand(AddCriteria);
            RemoveCriteriaCommand = new RelayCommand<AssignmentCriteria>(RemoveCriteria);

            SaveCommand = new AsyncRelayCommand(SaveAssignmentAsync);
            
            // Загружаем последний ответ студента, если есть
            LoadLastSubmissionAsync();
        }
        
        public void Initialize()
        {
            // Загружаем критерии, если это существующее задание
            if (_assignment.Id != 0 && _assignment.Criteria != null)
            {
                Criteria.Clear();
                foreach (var criteria in _assignment.Criteria)
                {
                    Criteria.Add(criteria);
                }
            }
        }

        private async Task SaveAssignmentAsync()
        {
            try
            {
                IsBusy = true;

                // Убедимся, что коллекция критериев в модели инициализирована
                if (_assignment.Criteria == null)
                {
                    _assignment.Criteria = new List<AssignmentCriteria>();
                }

                // Очищаем существующие критерии и добавляем из ViewModel
                _assignment.Criteria.Clear();
                foreach (var criteria in Criteria)
                {
                    _assignment.Criteria.Add(criteria);
                }

                // Сохраняем задание
                if (_assignment.Id == 0)
                {
                    await _courseService.CreateAssignmentAsync(_assignment);
                }
                else
                {
                    await _courseService.UpdateAssignmentAsync(_assignment);
                }
                
                // Обновляем ID задания после сохранения, если это было новое задание
                if (_assignment.Id == 0)
                {
                    // Перезагружаем задание из базы данных, чтобы получить его ID
                    var assignments = await _courseService.GetAssignmentsAsync(_assignment.CourseId);
                    var savedAssignment = assignments.FirstOrDefault(a => a.Title == _assignment.Title);
                    if (savedAssignment != null)
                    {
                        _assignment.Id = savedAssignment.Id;
                    }
                }

                // Закрываем диалог
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении задания: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void LoadLastSubmissionAsync()
        {
            try
            {
                IsBusy = true;

                var submission =
                    await _courseService.GetLastSubmissionAsync(_assignment.Id, _userService.CurrentUser.Id);
                if (submission != null)
                {
                    TextAnswer = submission.TextAnswer;
                    Status = submission.Status;

                    // Загружаем файлы
                    Files.Clear();
                    foreach (var file in submission.Files)
                    {
                        Files.Add(new FileInfo
                        {
                            FileName = file.FileName,
                            FilePath = file.FilePath,
                            Size = file.FileSize,
                            ContentType = file.ContentType,
                            UploadDate = file.UploadDate
                        });
                    }
                }
                else
                {
                    Status = SubmissionStatus.Draft;
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSubmit()
        {
            // Проверяем, можно ли отправить задание
            if (IsBusy) return false;
            if (Status == SubmissionStatus.Submitted || Status == SubmissionStatus.Reviewed) return false;

            // Проверяем, что есть текстовый ответ или файлы
            if (string.IsNullOrWhiteSpace(TextAnswer) && !Files.Any()) return false;

            // Проверяем, что не просрочен срок сдачи
            if (DateTime.Now > DueDate && !_assignment.AllowLateSubmissions) return false;

            return true;
        }

        private async Task SubmitAsync()
        {
            try
            {
                IsBusy = true;

                // Создаем объект ответа на задание
                var submission = new AssignmentSubmission
                {
                    AssignmentId = _assignment.Id,
                    UserId = _userService.CurrentUser.Id,
                    TextAnswer = TextAnswer,
                    SubmissionDate = DateTime.Now,
                    Status = SubmissionStatus.Submitted
                };

                // Сохраняем файлы, если они есть
                foreach (var file in Files)
                {
                    await _courseService.UploadFileAsync(_assignment.Id, file);
                }

                
                // Сохраняем критерии, если это новое задание
                if (_assignment.Id == 0)
                {
                    _assignment.Criteria = Criteria.ToList();
                }

                // Сохраняем задание, если это новое задание
                if (_assignment.Id == 0)
                {
                    await _courseService.CreateAssignmentAsync(_assignment);
                    submission.AssignmentId = _assignment.Id;
                }
                
                // Save submission
                await _courseService.SaveSubmissionAsync(submission);

                // Update status
                Status = SubmissionStatus.Submitted;

                // Show success animation
                await Task.Delay(500); // Simple delay instead of animation

                // Показываем сообщение об успехе
                MessageBox.Show("Задание успешно отправлено", "Успех", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке задания: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveDraftAsync()
        {
            try
            {
                IsBusy = true;

                // Создаем объект ответа на задание
                var submission = new AssignmentSubmission
                {
                    AssignmentId = _assignment.Id,
                    UserId = _userService.CurrentUser.Id,
                    TextAnswer = TextAnswer,
                    SubmissionDate = DateTime.Now,
                    Status = SubmissionStatus.Draft
                };

                // Сохраняем файлы, если они есть
                foreach (var file in Files)
                {
                    await _courseService.UploadFileAsync(_assignment.Id, file);
                }
                
                // Сохраняем критерии, если это новое задание
                if (_assignment.Id == 0)
                {
                    _assignment.Criteria = Criteria.ToList();
                }

                // Сохраняем задание, если это новое задание
                if (_assignment.Id == 0)
                {
                    await _courseService.CreateAssignmentAsync(_assignment);
                    submission.AssignmentId = _assignment.Id;
                }

                // Сохраняем ответ
                await _courseService.SaveSubmissionAsync(submission);

                // Обновляем статус
                Status = SubmissionStatus.Draft;

                // Показываем сообщение об успехе
                MessageBox.Show("Черновик успешно сохранен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении черновика: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void AddFile()
        {
            // Открываем диалог выбора файла
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = GetFileFilter()
            };

            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                var fileInfo = new System.IO.FileInfo(filePath);

                // Проверяем размер файла
                if (fileInfo.Length > _assignment.MaxFileSize * 1024 * 1024)
                {
                    MessageBox.Show($"Файл слишком большой. Максимальный размер: {_assignment.MaxFileSize} МБ",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем расширение файла
                var extension = System.IO.Path.GetExtension(filePath).ToLower();
                if (!_assignment.AllowedFileExtensions.Contains(extension))
                {
                    MessageBox.Show(
                        $"Недопустимый тип файла. Разрешены только: {string.Join(", ", _assignment.AllowedFileExtensions)}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Добавляем файл в список
                Files.Add(new FileInfo
                {
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    Size = fileInfo.Length,
                    ContentType = GetContentType(extension),
                    UploadDate = DateTime.Now
                });

                // Автоматически сохраняем черновик
                SaveDraftCommand.Execute(null);
            }
        }

        private void RemoveFile(FileInfo file)
        {
            if (file != null)
            {
                Files.Remove(file);

                // Автоматически сохраняем черновик
                SaveDraftCommand.Execute(null);
            }
        }

        private string GetFileFilter()
        {
            var extensions = string.Join(";", _assignment.AllowedFileExtensions.Select(ext => $"*{ext}"));
            return $"Разрешенные файлы ({extensions})|{extensions}|Все файлы (*.*)|*.*";
        }

        private string GetContentType(string extension)
        {
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }

        public void OnFileAdded(object sender, FileAddedEventArgs e)
        {
            Files.Add(e.File);
            Status = SubmissionStatus.Draft;
            SaveDraftCommand.Execute(null);
        }

        public void OnFileRemoved(object sender, FileRemovedEventArgs e)
        {
            var file = Files.FirstOrDefault(f => f.FileName == e.File.FileName);
            if (file != null)
            {
                Files.Remove(file);
                Status = SubmissionStatus.Draft;
                SaveDraftCommand.Execute(null);
            }
        }

        private void AddCriteria() {
            
            // Проверяем, что поля заполнены
            if (string.IsNullOrWhiteSpace(NewCriteriaDescription) || NewCriteriaMaxScore <= 0)
            {
                return;
            }
            
            var criteria = new AssignmentCriteria { 
                Description = NewCriteriaDescription, 
                MaxScore = NewCriteriaMaxScore, 
                AssignmentId = _assignment.Id 
            };

            // Добавляем в коллекцию ViewModel
            Criteria.Add(criteria);
    
            // Убедимся, что коллекция в модели инициализирована
            if (_assignment.Criteria == null) {
                _assignment.Criteria = new List<AssignmentCriteria>();
            }
    
            // Добавляем в модель
            _assignment.Criteria.Add(criteria);

            // Очищаем поля ввода
            NewCriteriaDescription = string.Empty;
            NewCriteriaMaxScore = 0;
            
            // Вызываем обновление UI
            OnPropertyChanged(nameof(Criteria));
        }

        private void RemoveCriteria(AssignmentCriteria criteria) {
            if (criteria != null) {
                Criteria.Remove(criteria);
        
                // Убедимся, что коллекция в модели инициализирована
                if (_assignment.Criteria != null) {
                    var criteriaToRemove = _assignment.Criteria.FirstOrDefault(c => 
                        c.Description == criteria.Description && 
                        c.MaxScore == criteria.MaxScore);
                
                    if (criteriaToRemove != null) {
                        _assignment.Criteria.Remove(criteriaToRemove);
                    }
                }
            }
        }
    }
}