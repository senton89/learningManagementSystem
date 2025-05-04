using System;

namespace WpfApp1.Models
{
    public class FileInfo
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string UniqueFileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public DateTime UploadDate { get; set; }
        public int UploaderId { get; set; }
        public User Uploader { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int? AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
        public string RelativePath { get; set; }
        public FileStatus Status { get; set; }
        public string Description { get; set; }
        public int DownloadCount { get; set; }
        public DateTime? LastDownloadDate { get; set; }
    }

    public enum FileStatus
    {
        Uploading,
        Available,
        Deleted
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public static FileValidationResult Success => new() { IsValid = true };
        public static FileValidationResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
    }

    public class FileUploadProgress
    {
        public string FileName { get; set; }
        public double Progress { get; set; }
        public long BytesUploaded { get; set; }
        public long TotalBytes { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public double UploadSpeed { get; set; } // bytes per second
        public FileUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; } // Добавлено поле ErrorMessage
    }

    public enum FileUploadStatus
    {
        Queued,
        Uploading,
        Completed,
        Failed,
        Cancelled
    }
}
