using System;

namespace WpfApp1.Models
{
    public class StudentProgress
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ContentId { get; set; }
        public ProgressStatus Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int? Score { get; set; }
        public string? Comment { get; set; }
        
        public virtual User User { get; set; }
        public virtual Content Content { get; set; }
    }

    public enum ProgressStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }
}
