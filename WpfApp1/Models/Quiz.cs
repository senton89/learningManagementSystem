using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace WpfApp1.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        public int? ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan TimeLimit { get; set; }
        public int PassingScore { get; set; }
        public bool RandomizeQuestions { get; set; }
        public bool ShowCorrectAnswers { get; set; }
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    }

    public class Question
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public int Points { get; set; }
        public string? ImageUrl { get; set; } = string.Empty;
        public string? Explanation { get; set; } = string.Empty;
        public virtual Quiz Quiz { get; set; }
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }

    public class Answer
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public virtual Question Question { get; set; }
    }

    public class QuizAttempt
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Score { get; set; }
        public bool IsPassed { get; set; }
        public virtual Quiz Quiz { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<QuestionResponse> Responses { get; set; } = new List<QuestionResponse>();
    }

    public class QuestionResponse
    {
        public int Id { get; set; }
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        
        [NotMapped]
        public virtual ICollection<int> SelectedAnswerIds { get; set; } = new List<int>();
        public string? TextResponse { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public virtual QuizAttempt Attempt { get; set; }
        public virtual Question Question { get; set; }
    }

    public enum QuestionType
    {
        SingleChoice,
        MultipleChoice,
        TrueFalse,
        ShortAnswer,
        Essay
    }

    public static class QuizExtensions
    {
        public static int CalculateScore(this QuizAttempt attempt)
        {
            if (!attempt.Responses.Any()) return 0;
    
            var totalPoints = attempt.Quiz.Questions.Sum(q => q.Points);
            var earnedPoints = attempt.Responses.Sum(r => r.PointsEarned);
    
            return totalPoints > 0
                ? (int)Math.Round((double)earnedPoints / totalPoints * 100)
                : 0;
        }

        public static bool CheckIsPassed(this QuizAttempt attempt)
        {
            return attempt.Score >= attempt.Quiz.PassingScore;
        }
    }
}
