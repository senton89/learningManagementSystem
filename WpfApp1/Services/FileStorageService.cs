using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WpfApp1.Models;
using FileInfo = WpfApp1.Models.FileInfo;

namespace WpfApp1.Services
{
    public class FileStorageService
    {
        private readonly string _baseStoragePath;
        private readonly CourseService _courseService;
        private readonly UserService _userService;

        public FileStorageService(
            string baseStoragePath,
            CourseService courseService,
            UserService userService)
        {
            _baseStoragePath = baseStoragePath ?? throw new ArgumentNullException(nameof(baseStoragePath));
            _courseService = courseService ?? throw new ArgumentNullException(nameof(courseService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    
            // Create base directory if it doesn't exist
            Directory.CreateDirectory(_baseStoragePath);
        }

        public async Task<FileInfo> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            long fileSize,
            int courseId,
            int? assignmentId = null,
            IProgress<double> progress = null,
            CancellationToken cancellationToken = default)
        {
            // Проверяем права доступа
            if (!await _courseService.CanUploadFilesAsync(courseId, _userService.CurrentUser.Id))
            {
                throw new UnauthorizedAccessException("У вас нет прав на загрузку файлов в этот курс");
            }

            // Генерируем уникальное имя файла
            var uniqueFileName = GenerateUniqueFileName(fileName);
            var relativePath = GetRelativePath(courseId, assignmentId);
            var fullPath = Path.Combine(_baseStoragePath, relativePath);
            
            // Создаем директорию, если она не существует
            Directory.CreateDirectory(fullPath);

            var fileInfo = new FileInfo
            {
                FileName = fileName,
                UniqueFileName = uniqueFileName,
                ContentType = contentType,
                Size = fileSize,
                UploadDate = DateTime.Now,
                UploaderId = _userService.CurrentUser.Id,
                CourseId = courseId,
                AssignmentId = assignmentId,
                RelativePath = Path.Combine(relativePath, uniqueFileName)
            };

            // Сохраняем файл
            var targetPath = Path.Combine(fullPath, uniqueFileName);
            await using (var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920]; // 80KB buffer
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await targetStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytesRead += bytesRead;
                    progress?.Report((double)totalBytesRead / fileSize);
                }
            }

            // Сохраняем информацию о файле в базу данных
            await _courseService.SaveFileInfoAsync(fileInfo);

            return fileInfo;
        }

        public async Task<Stream> DownloadFileAsync(
            string uniqueFileName,
            int courseId,
            int? assignmentId = null,
            IProgress<double> progress = null,
            CancellationToken cancellationToken = default)
        {
            // Проверяем права доступа
            if (!await _courseService.CanDownloadFilesAsync(courseId, _userService.CurrentUser.Id))
            {
                throw new UnauthorizedAccessException("У вас нет прав на скачивание файлов из этого курса");
            }

            var relativePath = GetRelativePath(courseId, assignmentId);
            var filePath = Path.Combine(_baseStoragePath, relativePath, uniqueFileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Файл не найден", filePath);
            }

            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public async Task DeleteFileAsync(string uniqueFileName, int courseId, int? assignmentId = null)
        {
            // Проверяем права доступа
            if (!await _courseService.CanDeleteFilesAsync(courseId, _userService.CurrentUser.Id))
            {
                throw new UnauthorizedAccessException("У вас нет прав на удаление файлов из этого курса");
            }

            var relativePath = GetRelativePath(courseId, assignmentId);
            var filePath = Path.Combine(_baseStoragePath, relativePath, uniqueFileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Удаляем информацию о файле из базы данных
            await _courseService.DeleteFileInfoAsync(uniqueFileName);
        }

        public async Task<List<FileInfo>> GetFilesAsync(int courseId, int? assignmentId = null)
        {
            // Проверяем права доступа
            if (!await _courseService.CanViewFilesAsync(courseId, _userService.CurrentUser.Id))
            {
                throw new UnauthorizedAccessException("У вас нет прав на просмотр файлов этого курса");
            }

            return await _courseService.GetFileInfosAsync(courseId, assignmentId);
        }

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var random = new Random().Next(1000, 9999);
            return $"{timestamp}_{random}{extension}";
        }

        private string GetRelativePath(int courseId, int? assignmentId)
        {
            var path = Path.Combine("courses", courseId.ToString());
            if (assignmentId.HasValue)
            {
                path = Path.Combine(path, "assignments", assignmentId.Value.ToString());
            }
            return path;
        }
    }
}
