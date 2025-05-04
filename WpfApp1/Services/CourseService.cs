using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.ViewModels;
using FileInfoModel = WpfApp1.Models.FileInfo; 

namespace WpfApp1.Services
{
    public class CourseService
    {
        private readonly LmsDbContext _context;
        private readonly AuditService _auditService;
        private readonly IDbContextFactory<LmsDbContext> _contextFactory;
        private readonly IConfiguration _configuration;
        private readonly UserService _userService; 

        public UserService UserService => _userService;
        
        public CourseService(LmsDbContext context, AuditService auditService, 
            IDbContextFactory<LmsDbContext> contextFactory, IConfiguration? configuration = null, UserService? userService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _configuration = configuration ?? new ConfigurationBuilder().Build();
            _userService = userService;
        }

        /// <summary>
        /// Создание нового курса
        /// </summary>
        public async Task<Course> CreateCourseAsync(Course course, int teacherId)
        {
            var teacher = await _context.Users.FindAsync(teacherId);
            if (teacher == null)
                throw new ArgumentException("Преподаватель не найден", nameof(teacherId));

            course.Teacher = teacher;
            course.CreationDate = DateTime.UtcNow;

            if (course.StartDate.HasValue)
                course.StartDate = DateTime.SpecifyKind(course.StartDate.Value, DateTimeKind.Utc);
    
            if (course.EndDate.HasValue)
                course.EndDate = DateTime.SpecifyKind(course.EndDate.Value, DateTimeKind.Utc);

            // Убедимся, что у модулей установлена связь с курсом
            if (course.Modules != null) {
                foreach (var module in course.Modules) {
                    module.CourseId = course.Id;
                }
            }
            
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(teacherId.ToString(), "CourseCreated",
                $"Создан курс: {course.Title}");

            return course;
        }

        /// <summary>
        /// Обновление курса
        /// </summary>
        public async Task<Course> UpdateCourseAsync(Course course, int teacherId)
        {
            var existingCourse = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == course.Id);

            if (existingCourse == null)
                throw new ArgumentException("Курс не найден", nameof(course.Id));

            if (existingCourse.Teacher?.Id != teacherId)
                throw new UnauthorizedAccessException("Только преподаватель курса может его редактировать");

            existingCourse.Title = course.Title;
            existingCourse.Description = course.Description;
            existingCourse.StartDate = course.StartDate;
            existingCourse.EndDate = course.EndDate;

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(teacherId.ToString(), "CourseUpdated",
                $"Обновлен курс: {course.Title}");

            return existingCourse;
        }
        
        /// <summary>
        /// Удаление курса
        /// </summary>
        public async Task DeleteCourseAsync(int courseId, int userId)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);
    
            if (course == null)
                throw new ArgumentException("Курс не найден", nameof(courseId));
    
            if (course.Teacher?.Id != userId)
                throw new UnauthorizedAccessException("Только преподаватель курса может его удалить");
    
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
    
            await _auditService.LogActionAsync(userId.ToString(), "CourseDeleted",
                $"Удален курс: {course.Title}");
        }

        /// <summary>
        /// Добавление модуля в курс
        /// </summary>
        public async Task<Module> AddModuleAsync(int courseId, Module module, int teacherId)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                throw new ArgumentException("Курс не найден", nameof(courseId));

            if (course.Teacher?.Id != teacherId)
                throw new UnauthorizedAccessException("Только преподаватель курса может добавлять модули");

            module.CourseId = course.Id;
            module.OrderIndex = (course.Modules?.Count ?? 0) + 1;

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(teacherId.ToString(), "ModuleAdded",
                $"Добавлен модуль '{module.Title}' в курс '{course.Title}'");

            return module;
        }

        /// <summary>
        /// Добавление контента в модуль
        /// </summary>
        public async Task<Content> AddContentAsync(int moduleId, Content content, int teacherId)
        {
            var module = await _context.Modules
                .Include(m => m.Course)
                .ThenInclude(c => c.Teacher)
                .Include(m => m.Contents)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                throw new ArgumentException("Модуль не найден", nameof(moduleId));

            if (module.Course?.Teacher?.Id != teacherId)
                throw new UnauthorizedAccessException("Только преподаватель курса может добавлять контент");

            content.ModuleId = module.Id;
            content.Module = module;
            content.OrderIndex = (module.Contents?.Count ?? 0) + 1;

            _context.Contents.Add(content);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(teacherId.ToString(), "ContentAdded",
                $"Добавлен контент '{content.Title}' в модуль '{module.Title}'");

            return content;
        }

        /// <summary>
        /// Запись студента на курс
        /// </summary>
        public async Task EnrollStudentAsync(int courseId, int studentId)
        {
            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                throw new ArgumentException("Курс не найден", nameof(courseId));

            var student = await _context.Users.FindAsync(studentId);
            if (student == null)
                throw new ArgumentException("Студент не найден", nameof(studentId));

            if (course.Students?.Any(s => s.Id == studentId) == true)
                throw new InvalidOperationException("Студент уже записан на курс");

            course.Students ??= new List<User>();
            course.Students.Add(student);

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(studentId.ToString(), "CourseEnrollment",
                $"Студент записан на курс '{course.Title}'");
        }

        /// <summary>
        /// Обновление прогресса студента
        /// </summary>
        public async Task UpdateProgressAsync(StudentProgress progress)
        {
            var existingProgress = await _context.Set<StudentProgress>()
                .FirstOrDefaultAsync(p => p.UserId == progress.UserId && p.ContentId == progress.ContentId);

            if (existingProgress == null)
            {
                progress.StartDate = DateTime.UtcNow;
                _context.Set<StudentProgress>().Add(progress);
            }
            else
            {
                existingProgress.Status = progress.Status;
                existingProgress.CompletionDate = progress.Status == ProgressStatus.Completed ? DateTime.UtcNow : null;
                existingProgress.Score = progress.Score;
                existingProgress.Comment = progress.Comment;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(progress.UserId.ToString(), "ProgressUpdated",
                $"Обновлен прогресс по контенту {progress.ContentId}");
        }

        /// <summary>
        /// Получение прогресса студента по курсу
        /// </summary>
        public async Task<IEnumerable<StudentProgress>> GetStudentProgressAsync(int courseId, int studentId)
        {
            return await _context.Set<StudentProgress>()
                .Include(p => p.Content)
                .ThenInclude(c => c.Module)
                .Where(p => p.User.Id == studentId && p.Content.Module.Course.Id == courseId)
                .OrderBy(p => p.Content.Module.OrderIndex)
                .ThenBy(p => p.Content.OrderIndex)
                .ToListAsync();
        }

        /// <summary>
        /// Получение статистики по курсу
        /// </summary>
        public async Task<CourseStatistics> GetCourseStatisticsAsync(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Students)
                .Include(c => c.Modules)
                .ThenInclude(m => m.Contents)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                throw new ArgumentException("Курс не найден", nameof(courseId));

            var progress = await _context.Set<StudentProgress>()
                .Include(p => p.Content)
                .Where(p => p.Content.Module.Course.Id == courseId)
                .ToListAsync();

            var totalStudents = course.Students?.Count ?? 0;
            var totalContent = course.Modules?.Sum(m => m.Contents?.Count ?? 0) ?? 0;

            var completedContent = progress
                .Where(p => p.Status == ProgressStatus.Completed)
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.Count());

            return new CourseStatistics
            {
                TotalStudents = totalStudents,
                ActiveStudents = progress.Select(p => p.UserId).Distinct().Count(),
                CompletedStudents = completedContent.Count(kvp => kvp.Value == totalContent),
                AverageProgress = totalStudents > 0
                    ? (double)progress.Count(p => p.Status == ProgressStatus.Completed) / (totalStudents * totalContent)
                    : 0,
                AverageScore = progress.Any(p => p.Score.HasValue)
                    ? progress.Where(p => p.Score.HasValue).Average(p => p.Score.Value)
                    : 0
            };
        }

        #region Assignment Management
        
        public async Task<List<Assignment>> GetAssignmentsAsync(int courseId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Assignments
                .Where(a => a.CourseId == courseId)
                .ToListAsync();
        }
        
        public async Task<Assignment> CreateAssignmentAsync(Assignment assignment)
        {
            using var context = _contextFactory.CreateDbContext();
    
            // Если у задания есть критерии, убедимся, что они правильно связаны
            if (assignment.Criteria != null)
            {
                foreach (var criteria in assignment.Criteria)
                {
                    criteria.AssignmentId = assignment.Id;
                }
            }
    
            context.Assignments.Add(assignment);
            await context.SaveChangesAsync();
    
            return assignment;
        }

        public async Task<Assignment> UpdateAssignmentAsync(Assignment assignment)
        {
            using var context = _contextFactory.CreateDbContext();
            var existing = await context.Assignments
                .Include(a => a.Criteria)
                .FirstOrDefaultAsync(a => a.Id == assignment.Id);

            if (existing == null)
                throw new InvalidOperationException("Задание не найдено");

            // Обновляем основные свойства
            context.Entry(existing).CurrentValues.SetValues(assignment);

            // Обновляем критерии
            existing.Criteria.Clear();
            if (assignment.Criteria != null) {
                foreach (var criteria in assignment.Criteria) {
                    existing.Criteria.Add(new AssignmentCriteria {
                        Description = criteria.Description,
                        MaxScore = criteria.MaxScore,
                        AssignmentId = existing.Id
                    });
                }
            }

            await context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAssignmentAsync(int assignmentId)
        {
            using var context = _contextFactory.CreateDbContext();
            var assignment = await context.Assignments.FindAsync(assignmentId);
            if (assignment != null)
            {
                context.Assignments.Remove(assignment);
                await context.SaveChangesAsync();
            }
        }

        public async Task<Assignment> GetAssignmentAsync(int assignmentId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Assignments
                .Include(a => a.Criteria)
                .Include(a => a.Submissions)
                .ThenInclude(s => s.Files)
                .Include(a => a.Submissions)
                .ThenInclude(s => s.CriteriaScores)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
        }

        #endregion

        #region Assignment Submission

        public async Task<AssignmentSubmission> GetLastSubmissionAsync(int assignmentId, int userId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.AssignmentSubmissions
                .Include(s => s.Files)
                .Include(s => s.CriteriaScores)
                .ThenInclude(cs => cs.Criteria)
                .Where(s => s.AssignmentId == assignmentId && s.UserId == userId)
                .OrderByDescending(s => s.SubmissionDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IList<AssignmentSubmission>> GetSubmissionsAsync(int assignmentId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.AssignmentSubmissions
                .Include(s => s.User)
                .Include(s => s.Files)
                .Include(s => s.CriteriaScores)
                .ThenInclude(cs => cs.Criteria)
                .Where(s => s.AssignmentId == assignmentId)
                .ToListAsync();
        }

        public async Task<AssignmentSubmission> SaveSubmissionAsync(AssignmentSubmission submission)
        {
            using var context = _contextFactory.CreateDbContext();

            if (submission.Id == 0)
            {
                // Новый ответ
                context.AssignmentSubmissions.Add(submission);
            }
            else
            {
                // Обновление существующего
                var existing = await context.AssignmentSubmissions
                    .Include(s => s.Files)
                    .Include(s => s.CriteriaScores)
                    .FirstOrDefaultAsync(s => s.Id == submission.Id);

                if (existing == null)
                    throw new InvalidOperationException("Ответ не найден");

                // Обновляем основные свойства
                context.Entry(existing).CurrentValues.SetValues(submission);

                // Обновляем файлы
                existing.Files.Clear();
                foreach (var file in submission.Files)
                {
                    // Копируем файл в хранилище
                    if (!string.IsNullOrEmpty(file.FilePath))
                    {
                        var fileName = Path.GetFileName(file.FilePath);
                        var storagePath = Path.Combine(
                            _configuration["Storage:AssignmentFiles"],
                            submission.AssignmentId.ToString(),
                            submission.UserId.ToString());

                        Directory.CreateDirectory(storagePath);
                        var targetPath = Path.Combine(storagePath, fileName);

                        File.Copy(file.FilePath, targetPath, true);
                        file.FilePath = targetPath;
                    }

                    existing.Files.Add(file);
                }

                // Обновляем оценки по критериям
                existing.CriteriaScores.Clear();
                foreach (var score in submission.CriteriaScores)
                {
                    existing.CriteriaScores.Add(score);
                }
            }

            await context.SaveChangesAsync();
            return submission;
        }

        public async Task<IList<AssignmentSubmission>> GetUserSubmissionsAsync(int userId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Files)
                .Include(s => s.CriteriaScores)
                .ThenInclude(cs => cs.Criteria)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SubmissionDate)
                .ToListAsync();
        }

        public async Task<IList<AssignmentSubmission>> GetPendingReviewsAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.AssignmentSubmissions
                .Include(s => s.User)
                .Include(s => s.Assignment)
                .Include(s => s.Files)
                .Where(s => s.Status == SubmissionStatus.Submitted)
                .OrderBy(s => s.SubmissionDate)
                .ToListAsync();
        }

        #endregion

        /// <summary>
        /// Checks if a user can upload files to a course
        /// </summary>
        public async Task<bool> CanUploadFilesAsync(int courseId, int userId)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return false;

            // Teachers can always upload files
            if (course.Teacher?.Id == userId)
                return true;

            // Check if user is enrolled in the course
            var isEnrolled = await _context.Courses
                .Where(c => c.Id == courseId)
                .SelectMany(c => c.Students)
                .AnyAsync(s => s.Id == userId);

            return isEnrolled;
        }

        /// <summary>
        /// Checks if a user can download files from a course
        /// </summary>
        public async Task<bool> CanDownloadFilesAsync(int courseId, int userId)
        {
            // Same permission logic as upload for now
            return await CanUploadFilesAsync(courseId, userId);
        }

        /// <summary>
        /// Checks if a user can delete files from a course
        /// </summary>
        public async Task<bool> CanDeleteFilesAsync(int courseId, int userId)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return false;

            // Only teachers and admins can delete files
            return course.Teacher?.Id == userId ||
                   (await _context.Users.FindAsync(userId))?.Role == UserRole.Administrator;
        }

        /// <summary>
        /// Checks if a user can view files in a course
        /// </summary>
        public async Task<bool> CanViewFilesAsync(int courseId, int userId)
        {
            // Same permission logic as download for now
            return await CanDownloadFilesAsync(courseId, userId);
        }

        /// <summary>
        /// Saves file information to the database
        /// </summary>
        public async Task<FileInfoModel> SaveFileInfoAsync(FileInfoModel fileInfo)
        {
            using var context = _contextFactory.CreateDbContext();

            // Check if this is an update or a new file
            var existingFile = await context.Set<FileInfoModel>()
                .FirstOrDefaultAsync(f => f.UniqueFileName == fileInfo.UniqueFileName);

            if (existingFile != null)
            {
                // Update existing file
                context.Entry(existingFile).CurrentValues.SetValues(fileInfo);
            }
            else
            {
                // Add new file
                await context.Set<FileInfoModel>().AddAsync(fileInfo);
            }

            await context.SaveChangesAsync();
            await _auditService.LogActionAsync(
                fileInfo.UploaderId.ToString(),
                "FileUploaded",
                $"File '{fileInfo.FileName}' uploaded to course {fileInfo.CourseId}");

            return fileInfo;
        }

        /// <summary>
        /// Retrieves file information for a course
        /// </summary>
        public async Task<List<FileInfoModel>> GetFileInfosAsync(int courseId, int? assignmentId = null)
        {
            using var context = _contextFactory.CreateDbContext();

            var query = context.Set<FileInfoModel>()
                .Where(f => f.CourseId == courseId && f.Status != FileStatus.Deleted);

            if (assignmentId.HasValue)
            {
                query = query.Where(f => f.AssignmentId == assignmentId.Value);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Deletes file information from the database
        /// </summary>
        public async Task DeleteFileInfoAsync(string uniqueFileName)
        {
            using var context = _contextFactory.CreateDbContext();

            var fileInfo = await context.Set<FileInfoModel>()
                .FirstOrDefaultAsync(f => f.UniqueFileName == uniqueFileName);

            if (fileInfo != null)
            {
                // Soft delete - just mark as deleted
                fileInfo.Status = FileStatus.Deleted;

                await context.SaveChangesAsync();
                await _auditService.LogActionAsync(
                    _userService.CurrentUser?.Id.ToString() ?? "System",
                    "FileDeleted",
                    $"File '{fileInfo.FileName}' deleted from course {fileInfo.CourseId}");
            }
        }

        public async Task<List<Course>> GetCoursesAsync(CourseFilter filter = CourseFilter.All,
            string searchQuery = null)
        {
            var query = _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                .Include(c => c.Modules)
                .ThenInclude(m => m.Contents).AsQueryable();

            // Apply filter
            if (filter != CourseFilter.All && _userService.CurrentUser != null)
            {
                var currentUserId = _userService.CurrentUser.Id;

                switch (filter)
                {
                    case CourseFilter.Teaching:
                        query = query.Where(c => c.TeacherId == currentUserId);
                        break;
                    case CourseFilter.Enrolled:
                        query = query.Where(c => c.Students.Any(s => s.Id == currentUserId));
                        break;
                    case CourseFilter.Available:
                        query = query.Where(c =>
                            c.Students.All(s => s.Id != currentUserId) && c.TeacherId != currentUserId);
                        break;
                }
            }

            // Apply search
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(c => c.Title.Contains(searchQuery) || c.Description.Contains(searchQuery));
            }

            return await query.ToListAsync();
        }

        public async Task<Course> GetCourseAsync(int courseId)
        {
            return await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                .Include(c => c.Modules)
                .ThenInclude(m => m.Contents)
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        public async Task<Module> UpdateModuleAsync(Module module, int teacherId)
        {
            var existingModule = await _context.Modules
                .Include(m => m.Course)
                .ThenInclude(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.Id == module.Id);

            if (existingModule == null)
                throw new ArgumentException("Module not found", nameof(module.Id));

            if (existingModule.Course?.Teacher?.Id != teacherId)
                throw new UnauthorizedAccessException("Only the course teacher can edit modules");

            existingModule.Title = module.Title;
            existingModule.Description = module.Description;
            existingModule.OrderIndex = module.OrderIndex;

            // Сохраняем связь с курсом
            existingModule.CourseId = existingModule.Course.Id;
            
            await _context.SaveChangesAsync();
            await _auditService.LogActionAsync(teacherId.ToString(), "ModuleUpdated",
                $"Updated module '{module.Title}' in course '{existingModule.Course.Title}'");

            return existingModule;
        }

        public async Task DeleteModuleAsync(int moduleId, int teacherId)
        {
            var module = await _context.Modules
                .Include(m => m.Course)
                .ThenInclude(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                throw new ArgumentException("Module not found", nameof(moduleId));

            if (module.Course?.Teacher?.Id != teacherId)
                throw new UnauthorizedAccessException("Only the course teacher can delete modules");

            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();
            await _auditService.LogActionAsync(teacherId.ToString(), "ModuleDeleted",
                $"Deleted module '{module.Title}' from course '{module.Course.Title}'");
        }

        public async Task<Models.NotificationSettings> GetNotificationSettingsAsync(int userId)
        {
            // This would typically be stored in a database table
            // For now, we'll return null to use default settings
            //TODO: implement settings stored in a database table
            return null;
        }

        public async Task<Quiz> GetQuizAsync(int contentId)
        {
            var content = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == contentId);

            if (content == null || content.Type != ContentType.Quiz)
                throw new ArgumentException("Quiz content not found", nameof(contentId));

            try
            {
                // Проверяем, что данные не пусты
                if (string.IsNullOrWhiteSpace(content.Data) || content.Data == "Enter content here")
                {
                    return new Quiz
                    {
                        Title = content.Title,
                        ContentId = content.Id,
                        Description = "Empty quiz content",
                        TimeLimit = TimeSpan.FromMinutes(30),
                        PassingScore = 70,
                        Questions = new List<Question>()
                    };
                }

                // Десериализуем quiz данные из content.Data
                var quiz = System.Text.Json.JsonSerializer.Deserialize<Quiz>(content.Data);

                // Если quiz null, создаем новый экземпляр
                if (quiz == null)
                {
                    quiz = new Quiz
                    {
                        Title = content.Title,
                        ContentId = content.Id,
                        Description = "Default quiz description",
                        TimeLimit = TimeSpan.FromMinutes(30),
                        PassingScore = 70,
                        Questions = new List<Question>()
                    };
                }
                
                // Проверяем, существует ли Quiz в базе данных
                var existingQuiz = await _context.Set<Quiz>()
                    .FirstOrDefaultAsync(q => q.ContentId == contentId);
            
                if (existingQuiz != null)
                {
                    // Если Quiz существует, обновляем его ID
                    quiz.Id = existingQuiz.Id;
                }

                return quiz;
            }
            catch (Exception ex)
            {
                // Логируем ошибку и возвращаем пустой Quiz
                Console.WriteLine($"Error deserializing quiz: {ex.Message}");
                return new Quiz
                {
                    Title = content.Title,
                    ContentId = content.Id,
                    Description = "Error loading quiz content",
                    TimeLimit = TimeSpan.FromMinutes(30),
                    PassingScore = 70,
                    Questions = new List<Question>()
                };
            }
        }
        
        public async Task<Quiz> SaveQuizAsync(Quiz quiz, int? contentId)
        {
            using var context = _contextFactory.CreateDbContext();
    
            // Получаем контент
            var content = await context.Contents.FindAsync(contentId);
            if (content == null || content.Type != ContentType.Quiz)
                throw new ArgumentException("Quiz content not found", nameof(contentId));
    
            // Сериализуем quiz в JSON и сохраняем в content.Data
            content.Data = System.Text.Json.JsonSerializer.Serialize(quiz);
    
            // Если это новый quiz, устанавливаем связь с контентом
            if (quiz.Id == 0)
            {
                quiz.ContentId = contentId;
                context.Set<Quiz>().Add(quiz);
            }
            else
            {
                context.Set<Quiz>().Update(quiz);
            }
    
            await context.SaveChangesAsync();
    
            // Возвращаем quiz с обновленным Id
            return quiz;
        }

        public async Task<QuizAttempt> SaveQuizAttemptAsync(QuizAttempt attempt)
        {
            // // Используем новый контекст для избежания проблем с отслеживанием
            // using var context = _contextFactory.CreateDbContext();
            //
            // // Проверяем, существует ли тест в базе данных
            // var existingQuiz = await context.Set<Quiz>()
            //     .Include(q => q.Questions)
            //     .ThenInclude(q => q.Answers)
            //     .FirstOrDefaultAsync(q => q.Id == attempt.QuizId);
            //
            // if (existingQuiz == null)
            // {
            //     // Если тест не найден, проверяем, есть ли он в попытке
            //     if (attempt.Quiz != null)
            //     {
            //         // Если Quiz новый (ID = 0), добавляем его в базу данных
            //         if (attempt.Quiz.Id == 0)
            //         {
            //             context.Set<Quiz>().Add(attempt.Quiz);
            //             await context.SaveChangesAsync();
            //             attempt.QuizId = attempt.Quiz.Id; // Обновляем QuizId в попытке
            //             existingQuiz = attempt.Quiz;
            //         }
            //         else
            //         {
            //             throw new InvalidOperationException($"Quiz with ID {attempt.QuizId} not found in the database.");
            //         }
            //     }
            //     else
            //     {
            //         throw new InvalidOperationException("Quiz not provided and not found in the database.");
            //     }
            // }
            var existingQuiz = attempt.Quiz;

            // Создаем новый список ответов с корректными ссылками
            var updatedResponses = new List<QuestionResponse>();

            foreach (var response in attempt.Responses)
            {
                if (response.QuestionId == 0 || response.Question?.Id == 0)
                {
                    // Найдем соответствующий вопрос в Quiz по тексту
                    var matchingQuestion = existingQuiz.Questions
                        .FirstOrDefault(q => q.Text == response.Question?.Text);

                    if (matchingQuestion != null)
                    {
                        response.QuestionId = matchingQuestion.Id;
                        response.Question = matchingQuestion;
                        updatedResponses.Add(response);
                        continue;
                    }
                }
                
                // Находим соответствующий вопрос из базы данных
                var question = existingQuiz.Questions.FirstOrDefault(q => q.Id == response.QuestionId);

                if (question != null)
                {
                    // Если вопрос найден, используем его
                    response.QuestionId = question.Id;
                    response.Question = question;
                    updatedResponses.Add(response);
                }
            }

            // Заменяем ответы на обновленные
            attempt.Responses.Clear();
            foreach (var response in updatedResponses)
            {
                attempt.Responses.Add(response);
            }
            
            // Рассчитываем оценку
            attempt.Score = attempt.CalculateScore();
            attempt.IsPassed = attempt.CheckIsPassed();

            // Проверяем, существует ли уже попытка для этого пользователя и теста
            var existingAttempt = await _context.Set<QuizAttempt>()
                .Include(a => a.Responses)
                .FirstOrDefaultAsync(a => a.QuizId == attempt.QuizId && a.UserId == attempt.UserId);

            if (existingAttempt != null) {
                // Обновляем существующую попытку
                existingAttempt.EndTime = attempt.EndTime;
                existingAttempt.Score = attempt.Score;
                existingAttempt.IsPassed = attempt.IsPassed;
        
                // Обновляем ответы
                existingAttempt.Responses.Clear();
                foreach (var response in attempt.Responses) {
                    existingAttempt.Responses.Add(response);
                }
            } else {
                // Добавляем новую попытку
                _context.Set<QuizAttempt>().Add(attempt);
            }

            await _context.SaveChangesAsync();
    
            // Возвращаем сохраненную попытку
            return existingAttempt ?? attempt;
        }

        public async Task<FileInfoModel> UploadFileAsync(int assignmentId, FileInfoModel file)
        {
            // Ensure the assignment exists
            var assignment = await _context.Assignments.FindAsync(assignmentId);
            if (assignment == null)
                throw new ArgumentException("Assignment not found", nameof(assignmentId));

            // Set assignment ID
            file.AssignmentId = assignmentId;
            file.Status = FileStatus.Available;

            // Save file info to database
            return await SaveFileInfoAsync(file);
        }

        public async Task DeleteFileAsync(int assignmentId, string fileName)
        {
            var file = await _context.Set<FileInfoModel>()
                .FirstOrDefaultAsync(f => f.AssignmentId == assignmentId && f.FileName == fileName);

            if (file != null)
            {
                await DeleteFileInfoAsync(file.UniqueFileName);
            }
        }
        /// <summary>
        /// Проверяет, имеет ли пользователь доступ к курсу
        /// </summary>
        public async Task<bool> CanAccessCourseAsync(int courseId, int userId)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == courseId);
        
            if (course == null)
                return false;
        
            // Преподаватель курса всегда имеет доступ
            if (course.Teacher?.Id == userId)
                return true;
        
            // Администратор всегда имеет доступ
            var user = await _context.Users.FindAsync(userId);
            if (user?.Role == UserRole.Administrator)
                return true;
        
            // Проверяем, записан ли студент на курс
            return course.Students?.Any(s => s.Id == userId) == true;
        }
    }
    


    public class CourseStatistics
    {
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int CompletedStudents { get; set; }
        public double AverageProgress { get; set; }
        public double AverageScore { get; set; }
    }
}
