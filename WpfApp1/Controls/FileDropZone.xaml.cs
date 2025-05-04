using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WpfApp1.Models;

namespace WpfApp1.Controls
{
    public partial class FileDropZone : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty MaxFileSizeProperty =
            DependencyProperty.Register(nameof(MaxFileSize), typeof(long), typeof(FileDropZone), 
                new PropertyMetadata(10L * 1024 * 1024)); // 10MB по умолчанию

        public static readonly DependencyProperty AllowedExtensionsProperty =
            DependencyProperty.Register(nameof(AllowedExtensions), typeof(string[]), typeof(FileDropZone), 
                new PropertyMetadata(new string[] { ".pdf", ".doc", ".docx", ".txt" }));

        public static readonly DependencyProperty MaxFilesProperty =
            DependencyProperty.Register(nameof(MaxFiles), typeof(int), typeof(FileDropZone), 
                new PropertyMetadata(10));

        private bool _isDraggingOver;
        public bool IsDraggingOver
        {
            get => _isDraggingOver;
            set
            {
                _isDraggingOver = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDraggingOver)));
            }
        }

        public ObservableCollection<FileUploadProgress> UploadingFiles { get; } = new();

        public bool HasUploadingFiles => UploadingFiles.Any();

        public long MaxFileSize
        {
            get => (long)GetValue(MaxFileSizeProperty);
            set => SetValue(MaxFileSizeProperty, value);
        }

        public string[] AllowedExtensions
        {
            get => (string[])GetValue(AllowedExtensionsProperty);
            set => SetValue(AllowedExtensionsProperty, value);
        }

        public int MaxFiles
        {
            get => (int)GetValue(MaxFilesProperty);
            set => SetValue(MaxFilesProperty, value);
        }

        public event EventHandler<FileValidationEventArgs> FileValidationFailed;
        public event EventHandler<FileUploadEventArgs> FileUploadStarted;
        public event EventHandler<FileUploadEventArgs> FileUploadCompleted;
        public event EventHandler<FileUploadEventArgs> FileUploadFailed;
        public event EventHandler<FileUploadEventArgs> FileUploadCancelled;

        public FileDropZone()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                IsDraggingOver = true;
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            IsDraggingOver = false;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            IsDraggingOver = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ProcessFiles(files);
            }

            e.Handled = true;
        }

        private void OnSelectFilesClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = GetFileFilter()
            };

            if (dialog.ShowDialog() == true)
            {
                ProcessFiles(dialog.FileNames);
            }
        }

        private string GetFileFilter()
        {
            var extensions = string.Join(";", AllowedExtensions.Select(ext => $"*{ext}"));
            return $"Разрешенные файлы ({extensions})|{extensions}|Все файлы (*.*)|*.*";
        }

        private void ProcessFiles(string[] files)
        {
            foreach (var file in files)
            {
                var fileInfo = new System.IO.FileInfo(file);
                var validationResult = ValidateFile(fileInfo);

                if (!validationResult.IsValid)
                {
                    FileValidationFailed?.Invoke(this, new FileValidationEventArgs(fileInfo.Name, validationResult.ErrorMessage));
                    continue;
                }

                var progress = new FileUploadProgress
                {
                    FileName = fileInfo.Name,
                    TotalBytes = fileInfo.Length,
                    Status = FileUploadStatus.Queued
                };

                UploadingFiles.Add(progress);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUploadingFiles)));

                FileUploadStarted?.Invoke(this, new FileUploadEventArgs(progress));
            }
        }

        private FileValidationResult ValidateFile(System.IO.FileInfo file)
        {
            // Проверка размера
            if (file.Length > MaxFileSize)
            {
                return FileValidationResult.Error(
                    $"Файл слишком большой. Максимальный размер: {MaxFileSize / 1024 / 1024}MB");
            }

            // Проверка расширения
            var extension = file.Extension.ToLower();
            if (!AllowedExtensions.Contains(extension))
            {
                return FileValidationResult.Error(
                    $"Недопустимый тип файла. Разрешены только: {string.Join(", ", AllowedExtensions)}");
            }

            // Проверка количества файлов
            if (UploadingFiles.Count >= MaxFiles)
            {
                return FileValidationResult.Error(
                    $"Превышено максимальное количество файлов ({MaxFiles})");
            }

            return FileValidationResult.Success;
        }

        public void UpdateProgress(string fileName, double progress)
        {
            var file = UploadingFiles.FirstOrDefault(f => f.FileName == fileName);
            if (file != null)
            {
                file.Progress = progress * 100;
                file.BytesUploaded = (long)(file.TotalBytes * progress);
                file.Status = FileUploadStatus.Uploading;
            }
        }

        public void CompleteUpload(string fileName)
        {
            var file = UploadingFiles.FirstOrDefault(f => f.FileName == fileName);
            if (file != null)
            {
                file.Progress = 100;
                file.BytesUploaded = file.TotalBytes;
                file.Status = FileUploadStatus.Completed;
                FileUploadCompleted?.Invoke(this, new FileUploadEventArgs(file));

                UploadingFiles.Remove(file);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUploadingFiles)));
            }
        }

        public void FailUpload(string fileName, string error)
        {
            var file = UploadingFiles.FirstOrDefault(f => f.FileName == fileName);
            if (file != null)
            {
                file.Status = FileUploadStatus.Failed;
                file.ErrorMessage = error;
                FileUploadFailed?.Invoke(this, new FileUploadEventArgs(file));

                UploadingFiles.Remove(file);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUploadingFiles)));
            }
        }

        public void CancelUpload(string fileName)
        {
            var file = UploadingFiles.FirstOrDefault(f => f.FileName == fileName);
            if (file != null)
            {
                file.Status = FileUploadStatus.Cancelled;
                FileUploadCancelled?.Invoke(this, new FileUploadEventArgs(file));

                UploadingFiles.Remove(file);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUploadingFiles)));
            }
        }
        
    }

    public class FileValidationEventArgs : EventArgs
    {
        public string FileName { get; }
        public string ErrorMessage { get; }

        public FileValidationEventArgs(string fileName, string errorMessage)
        {
            FileName = fileName;
            ErrorMessage = errorMessage;
        }
    }

    public class FileUploadEventArgs : EventArgs
    {
        public FileUploadProgress File { get; }

        public FileUploadEventArgs(FileUploadProgress file)
        {
            File = file;
        }
    }
}
