using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using WpfApp1.Models;

namespace WpfApp1.Infrastructure
{
    public class LmsDbContext : DbContext 
    { 
        public DbSet<User> Users { get; set; } = null!; 
        public DbSet<Course> Courses { get; set; } = null!; 
        public DbSet<Module> Modules { get; set; } = null!; 
        public DbSet<Content> Contents { get; set; } = null!; 
        public DbSet<Permission> Permissions { get; set; } = null!; 
        public DbSet<AuditLog> AuditLogs { get; set; } = null!; 
        public DbSet<UserPermission> UserPermissions { get; set; } = null!; 
        public DbSet<UserCredentials> UserCredentials { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; } = null!;
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; } = null!;
        public DbSet<AssignmentCriteria> AssignmentCriteria { get; set; } = null!;
        public DbSet<SubmissionFile> SubmissionFiles { get; set; } = null!;
        public DbSet<CriteriaScore> CriteriaScores { get; set; } = null!;
        public DbSet<PermissionRole> PermissionRoles { get; set; } = null!;
        public DbSet<AssignmentCheck> AssignmentChecks { get; set; } = null!;
        public DbSet<StudentProgress> StudentProgresses { get; set; } = null!;
        public DbSet<QuizAttempt> QuizAttempts { get; set; } = null!;

        public LmsDbContext(DbContextOptions<LmsDbContext> options) : base(options)
        {
        }

        public LmsDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Загружаем конфигурацию из appsettings.json для миграций
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Используем резервную строку подключения, если не найдена в конфигурации
                    connectionString = "Host=localhost;Database=lms;Username=postgres;Password=postgres";
                }
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

                optionsBuilder.UseNpgsql(connectionString);
            }
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        { 
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Role).HasConversion<string>();
                entity.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(u => u.LastName).HasMaxLength(50).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(100).IsRequired();
                
                // Многие-ко-многим между пользователями и разрешениями 
                entity.HasMany(u => u.CustomPermissions)
                    .WithMany()
                    .UsingEntity<UserPermission>(
                        j => j.HasOne(up => up.Permission)
                            .WithMany()
                            .HasForeignKey(up => up.PermissionId),
                        j => j.HasOne(up => up.User)
                            .WithMany()
                            .HasForeignKey(up => up.UserId),
                        j => {
                            j.HasKey(up => new { up.UserId, up.PermissionId });
                            j.ToTable("UserPermissions");
                        });

                entity.HasOne(u => u.Credentials)
                    .WithOne(c => c.User)
                    .HasForeignKey<UserCredentials>(c => c.UserId);
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.Navigation(c => c.Modules)
                    .AutoInclude(false);
                // Явно указываем отношение между Course и User для свойства Teacher
                entity.HasOne(c => c.Teacher)
                    .WithMany(u => u.TeachingCourses)
                    .HasForeignKey("TeacherId")  // Добавляем внешний ключ
                    .IsRequired(false);  // Делаем отношение необязательным

                // Многие-ко-многим для Students
                entity.HasMany(c => c.Students)
                    .WithMany(u => u.EnrolledCourses)
                    .UsingEntity(j => j.ToTable("CourseStudents"));
            });

            modelBuilder.Entity<Module>(entity =>
            {
                entity.Navigation(m => m.Course)
                    .AutoInclude(false);
                entity.HasOne(m => m.Course)
                    .WithMany(c => c.Modules)
                    .HasForeignKey("CourseId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Content>(entity =>
            {
                entity.HasOne(c => c.Module)
                    .WithMany(m => m.Contents)
                    .HasForeignKey("ModuleId");
                entity.Property(c => c.Type).HasConversion<string>();
            });
            
            modelBuilder.Entity<StudentProgress>(entity =>
            {
                entity.HasKey(sp => sp.Id);
    
                entity.HasOne(sp => sp.User)
                    .WithMany()
                    .HasForeignKey(sp => sp.UserId);
        
                entity.HasOne(sp => sp.Content)
                    .WithMany()
                    .HasForeignKey(sp => sp.ContentId);
        
                entity.Property(sp => sp.Status)
                    .HasConversion<string>();
            });
            
            modelBuilder.Entity<QuizAttempt>(entity =>
            {
                entity.HasOne(qa => qa.Quiz)
                    .WithMany()
                    .HasForeignKey(qa => qa.QuizId);
        
                entity.HasOne(qa => qa.User)
                    .WithMany()
                    .HasForeignKey(qa => qa.UserId);
        
                entity.HasMany(qa => qa.Responses)
                    .WithOne(qr => qr.Attempt)
                    .HasForeignKey(qr => qr.AttemptId);
            });

            modelBuilder.Entity<QuestionResponse>(entity =>
            {
                entity.HasOne(qr => qr.Question)
                    .WithMany()
                    .HasForeignKey(qr => qr.QuestionId);
            });
            
            modelBuilder.Entity<Quiz>()
                .HasOne<Content>()
                .WithOne()
                .HasForeignKey<Quiz>(q => q.ContentId)
                .IsRequired(false);

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.Type).HasConversion<string>();
                entity.Property(p => p.ResourceType).HasMaxLength(100);
    
                // Настраиваем связь с PermissionRole
                entity.HasMany(p => p.ApplicableRoles)
                    .WithOne(pr => pr.Permission)
                    .HasForeignKey(pr => pr.PermissionId);
            });
            modelBuilder.Entity<PermissionRole>(entity =>
            {
                entity.HasKey(pr => pr.Id);
                entity.Property(pr => pr.Role).HasMaxLength(50).IsRequired();
            });
            
            modelBuilder.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.Permissions)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.Users)
                .HasForeignKey(up => up.PermissionId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<UserCredentials>(entity =>
            {
                entity.HasKey(c => c.UserId);
    
                entity.HasIndex(c => c.PasswordResetToken)
                    .IsUnique()
                    .HasFilter("\"PasswordResetToken\" IS NOT NULL");  // Используем двойные кавычки вместо квадратных скобок
    
                entity.HasOne(c => c.User)
                    .WithOne(u => u.Credentials)
                    .HasForeignKey<UserCredentials>(c => c.UserId);
            });

            // Настройка для Assignment и связанных сущностей
            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.HasMany(a => a.Submissions)
                    .WithOne(s => s.Assignment)
                    .HasForeignKey(s => s.AssignmentId);
                
                entity.HasMany(a => a.Criteria)
                    .WithOne(c => c.Assignment)
                    .HasForeignKey(c => c.AssignmentId);
            });

            modelBuilder.Entity<AssignmentSubmission>(entity =>
            {
                entity.HasMany(s => s.Files)
                    .WithOne(f => f.Submission)
                    .HasForeignKey(f => f.SubmissionId);
                
                entity.HasMany(s => s.CriteriaScores)
                    .WithOne(c => c.Submission)
                    .HasForeignKey(c => c.SubmissionId);
                
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId);
            });

            modelBuilder.Entity<CriteriaScore>(entity =>
            {
                entity.HasOne(c => c.Criteria)
                    .WithMany()
                    .HasForeignKey(c => c.CriteriaId);
            });
            
            modelBuilder.Entity<AssignmentCheck>(entity =>
            {
                entity.HasKey(ac => ac.Id);
        
                entity.HasOne(ac => ac.Assignment)
                    .WithMany(a => a.Checks)
                    .HasForeignKey(ac => ac.AssignmentId);
            });
        }
    }
}