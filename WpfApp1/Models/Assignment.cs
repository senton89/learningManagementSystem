using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime DueDate { get; set; }
        public int MaxScore { get; set; }
        public bool RequiresFileUpload { get; set; }
        public List<string> AllowedFileExtensions { get; set; } = new();
        public int MaxFileSize { get; set; } // в мегабайтах
        public bool AllowLateSubmissions { get; set; }
        public int? LatePenaltyPerDay { get; set; } // процент снижения максимального балла
        public List<AssignmentCriteria> Criteria { get; set; } = new();
        public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
        
        public List<AssignmentCheck> Checks { get; set; } = new();
    }

    public class AssignmentCriteria
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int MaxScore { get; set; }
        public virtual Assignment Assignment { get; set; }
    }

    public class AssignmentSubmission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int UserId { get; set; }
        public string TextAnswer { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public SubmissionStatus Status { get; set; }
        public int? Score { get; set; }
        public string? TeacherComment { get; set; }
        public DateTime? ReviewDate { get; set; }
        public virtual Assignment Assignment { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
        public virtual ICollection<CriteriaScore> CriteriaScores { get; set; } = new List<CriteriaScore>();

        public bool IsLate => Assignment != null && SubmissionDate > Assignment.DueDate;
        public int CalculatePenalty() 
        { 
            if (!IsLate || Assignment == null || !Assignment.LatePenaltyPerDay.HasValue) 
                return 0;
    
            var daysLate = (int)(SubmissionDate - Assignment.DueDate).TotalDays;
            return Math.Min(100, daysLate * Assignment.LatePenaltyPerDay.Value);
        }
    }

    public class SubmissionFile
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public virtual AssignmentSubmission Submission { get; set; }
    }

    public class CriteriaScore
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public int CriteriaId { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
        public virtual AssignmentSubmission Submission { get; set; }
        public virtual AssignmentCriteria Criteria { get; set; }
    }
    
    public class AssignmentCheck 
    {
        [Key]
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        
        // Навигационное свойство
        public virtual Assignment Assignment { get; set; }
    }

    public enum SubmissionStatus
    {
        Draft,
        Submitted,
        UnderReview,
        Reviewed,
        RequiresRevision
    }

}
