using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using CommunityToolkit.WinUI.Notifications;
using WpfApp1.Models;
using ToastAudio = CommunityToolkit.WinUI.Notifications.ToastAudio;
using ToastContentBuilder = CommunityToolkit.WinUI.Notifications.ToastContentBuilder;

namespace WpfApp1.Services
{
    public class NotificationService
    {
        private readonly CourseService _courseService;
        private readonly UserService _userService;
        private readonly System.Windows.Threading.DispatcherTimer _checkTimer;
        private readonly Dictionary<int, DateTime> _lastNotificationTime;
        private NotificationSettings? _settings;

        public NotificationService(CourseService courseService, UserService userService)
        {
            _courseService = courseService;
            _userService = userService;
            _lastNotificationTime = new Dictionary<int, DateTime>();

            // Initialize timer for deadline checks
            _checkTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // Check every 5 minutes
            };
            _checkTimer.Tick += async (s, e) => await CheckDeadlinesAsync();

            // Load settings when service is created
            // LoadSettingsAsync();
        }
        
        public async Task InitializeAsync()
        {
            // Проверяем, что пользователь установлен
            if (_userService.CurrentUser == null)
            {
                return;
            }
        
            try
            {
                _settings = await GetNotificationSettingsAsync(_userService.CurrentUser.Id);

                if (_settings == null)
                {
                    _settings = CreateDefaultSettings();
                }
            
                // Start timer only if notifications are enabled
                if (_settings.NotificationsEnabled)
                {
                    _checkTimer.Start();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем MessageBox, чтобы не блокировать UI
                System.Diagnostics.Debug.WriteLine($"Error loading notification settings: {ex.Message}");
            }
        }

        private async void LoadSettingsAsync()
        {
            try
            {
                // Implement this method in CourseService
                // _settings = await GetNotificationSettingsAsync(_userService.CurrentUser.Id);
                
                _settings = await GetNotificationSettingsAsync(_userService.CurrentUser.Id);
                
                if (_settings == null)
                {
                    _settings = CreateDefaultSettings();
                    await SaveNotificationSettingsAsync(_settings);
                }
                
                // Start timer only if notifications are enabled
                if (_settings.NotificationsEnabled)
                {
                    _checkTimer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading notification settings: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private NotificationSettings CreateDefaultSettings()
        {
            return new NotificationSettings
            {
                UserId = _userService.CurrentUser.Id,
                NotificationsEnabled = true,
                SoundEnabled = true,
                QuietHoursEnabled = true,
                QuietHoursStart = new TimeSpan(23, 0, 0), // 11:00 PM
                QuietHoursEnd = new TimeSpan(7, 0, 0),    // 7:00 AM
                TypeSettings = new Dictionary<NotificationType, NotificationTypeSettings>
                {
                    { NotificationType.DeadlineApproaching, new NotificationTypeSettings
                        {
                            Enabled = true,
                            Priority = NotificationPriority.High,
                            DeliveryMethod = NotificationDeliveryMethod.Toast | NotificationDeliveryMethod.Email
                        }
                    },
                    { NotificationType.DeadlineMissed, new NotificationTypeSettings
                        {
                            Enabled = true,
                            Priority = NotificationPriority.High,
                            DeliveryMethod = NotificationDeliveryMethod.Toast | NotificationDeliveryMethod.Email
                        }
                    },
                    { NotificationType.AssignmentGraded, new NotificationTypeSettings
                        {
                            Enabled = true,
                            Priority = NotificationPriority.Normal,
                            DeliveryMethod = NotificationDeliveryMethod.Toast
                        }
                    }
                }
            };
        }

        public async Task CheckDeadlinesAsync()
        {
            try
            {
                if (_settings == null || !_settings.NotificationsEnabled)
                    return;

                // Check quiet hours
                if (_settings.QuietHoursEnabled)
                {
                    var nowTimeOfDay = DateTime.Now.TimeOfDay;
                    if (IsInQuietHours(nowTimeOfDay))
                        return;
                }

                // Get active assignments for the current user
                var assignments = await GetActiveAssignmentsAsync(_userService.CurrentUser.Id);
                var now = DateTime.Now;

                foreach (var assignment in assignments)
                {
                    // Skip if already submitted or reviewed
                    if (assignment.Submissions.Any(s => s.Status == SubmissionStatus.Submitted || 
                        s.Status == SubmissionStatus.Reviewed))
                    {
                        continue;
                    }

                    var timeUntil = assignment.DueDate - now;

                    // Determine notification type
                    NotificationType? notificationType = null;
                    string? message = null;

                    if (timeUntil.TotalDays <= 0)
                    {
                        notificationType = NotificationType.DeadlineMissed;
                        message = $"Deadline for assignment '{assignment.Title}' has passed";
                    }
                    else if (timeUntil.TotalDays <= 1)
                    {
                        notificationType = NotificationType.DeadlineApproaching;
                        message = $"Assignment '{assignment.Title}' is due in {(int)timeUntil.TotalHours} hours";
                    }
                    else if (timeUntil.TotalDays <= 3 && !_lastNotificationTime.ContainsKey(assignment.Id))
                    {
                        notificationType = NotificationType.DeadlineApproaching;
                        message = $"Assignment '{assignment.Title}' is due in {(int)timeUntil.TotalDays} days";
                    }

                    if (notificationType.HasValue && message != null && 
                        ShouldShowNotification(assignment.Id, notificationType.Value))
                    {
                        await ShowNotificationAsync(notificationType.Value, message, assignment);
                        _lastNotificationTime[assignment.Id] = now;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking deadlines: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsInQuietHours(TimeSpan currentTime)
        {
            if (_settings == null)
                return false;
                
            if (_settings.QuietHoursStart < _settings.QuietHoursEnd)
            {
                return currentTime >= _settings.QuietHoursStart && 
                       currentTime <= _settings.QuietHoursEnd;
            }
            else
            {
                return currentTime >= _settings.QuietHoursStart || 
                       currentTime <= _settings.QuietHoursEnd;
            }
        }

        private bool ShouldShowNotification(int assignmentId, NotificationType type)
        {
            if (_settings == null)
                return false;
                
            // Check notification type settings
            if (!_settings.TypeSettings.TryGetValue(type, out var typeSettings) || !typeSettings.Enabled)
                return false;

            // Check when the last notification was sent
            if (_lastNotificationTime.TryGetValue(assignmentId, out var lastTime))
            {
                var timeSinceLastNotification = DateTime.Now - lastTime;

                // Determine minimum interval between notifications based on priority
                var minInterval = typeSettings.Priority switch
                {
                    NotificationPriority.Low => TimeSpan.FromHours(24),
                    NotificationPriority.Normal => TimeSpan.FromHours(12),
                    NotificationPriority.High => TimeSpan.FromHours(4),
                    _ => TimeSpan.FromHours(12)
                };

                return timeSinceLastNotification >= minInterval;
            }

            return true;
        }

        private async Task ShowNotificationAsync(NotificationType type, string message, Assignment assignment)
        {
            if (_settings == null)
                return;
                
            var typeSettings = _settings.TypeSettings[type];

            // Show Toast notification
if (typeSettings.DeliveryMethod.HasFlag(NotificationDeliveryMethod.Toast) && _settings.NotificationsEnabled)
            {
                var builder = new ToastContentBuilder()
                    .AddText(message)
                    .AddText("Click to open assignment");

                if (_settings.SoundEnabled)
                {
                    builder.AddAudio(new ToastAudio
                    {
                        Src = new Uri("ms-appx:///Assets/notification.wav"),
                        Loop = false,
                        Silent = false
                    });
                }

                // Add arguments for handling the notification when clicked
                builder.AddArgument("action", "openAssignment")
                       .AddArgument("assignmentId", assignment.Id.ToString())
                       .AddArgument("courseId", assignment.CourseId.ToString());

                // Show the notification
                // builder.Show();
                /*var toastContent = builder.GetToastContent();
                var toast = new ToastNotification(toastContent.GetXml());
                ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);*/
            }

            // Send email notification
            if (typeSettings.DeliveryMethod.HasFlag(NotificationDeliveryMethod.Email))
            {
                await SendEmailNotificationAsync(type, message, assignment);
            }
        }

        private async Task SendEmailNotificationAsync(NotificationType type, string message, Assignment assignment)
        {
            var user = _userService.CurrentUser;
            var subject = type switch
            {
                NotificationType.DeadlineApproaching => "Assignment deadline approaching",
                NotificationType.DeadlineMissed => "Assignment deadline missed",
                _ => "Assignment notification"
            };

            var body = string.Format(
                "Hello, {0}!\n\n{1}\n\nAssignment details:\n- Title: {2}\n- Description: {3}\n- Due date: {4:dd.MM.yyyy HH:mm}\n\nClick the link to open the assignment:\n{5}\n\nRegards,\nLMS System",
                user.FullName,
                message,
                assignment.Title,
                assignment.Description,
                assignment.DueDate,
                GetAssignmentUrl(assignment.Id));

            await SendEmailAsync(user.Email, subject, body);
        }

        private string GetAssignmentUrl(int assignmentId)
        {
            return $"coursemanager://assignment/{assignmentId}";
        }

        public async Task UpdateSettingsAsync(NotificationSettings settings)
        {
            _settings = settings;
            await SaveNotificationSettingsAsync(settings);

            // Update timer state
            if (settings.NotificationsEnabled)
            {
                _checkTimer.Start();
            }
            else
            {
                _checkTimer.Stop();
            }
        }

        public void Start()
        {
            if (_settings?.NotificationsEnabled == true)
            {
                _checkTimer.Start();
            }
        }

        public void Stop()
        {
            _checkTimer.Stop();
        }

        // Method to send a notification manually (for testing)
        public async Task SendNotificationAsync(Notification notification)
        {
            var builder = new ToastContentBuilder()
                .AddText(notification.Title)
                .AddText(notification.Message);

            if (_settings?.SoundEnabled == true)
            {
                builder.AddAudio(new ToastAudio
                {
                    Src = new Uri("ms-appx:///Assets/notification.wav"),
                    Loop = false,
                    Silent = false
                });
            }

            builder.AddArgument("action", "test")
                   .AddArgument("type", notification.Type.ToString());

            // builder.Show();
            //TODO: implement
            /*var toastContent = builder.GetToastContent();
            var toast = new ToastNotification(toastContent.GetXml());
            ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);*/
        }

        // Helper methods that should be implemented in CourseService
        private async Task<NotificationSettings?> GetNotificationSettingsAsync(int userId)
        {
            // This should be implemented in CourseService
            // For now, we'll return null to use default settings
            return null;
        }

        private async Task SaveNotificationSettingsAsync(NotificationSettings settings)
        {
            // This should be implemented in CourseService
            // For now, we'll just log to console
            Console.WriteLine($"Saving notification settings for user {settings.UserId}");
        }

        private async Task<List<Assignment>> GetActiveAssignmentsAsync(int userId)
        {
            // This should be implemented in CourseService
            // For now, return an empty list
            return new List<Assignment>();
        }

        private async Task SendEmailAsync(string email, string subject, string body)
        {
            // This should be implemented in CourseService or a dedicated EmailService
            // For now, just log to console
            Console.WriteLine($"Sending email to {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body: {body}");
        }
    }

    // Supporting classes and enums
    public enum NotificationType
    {
        DeadlineApproaching,
        DeadlineMissed,
        AssignmentGraded,
        System
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High
    }

    [Flags]
    public enum NotificationDeliveryMethod
    {
        None = 0,
        Toast = 1,
        Email = 2
    }

    public class NotificationSettings
    {
        public int UserId { get; set; }
        public bool NotificationsEnabled { get; set; }
        public bool SoundEnabled { get; set; }
        public bool QuietHoursEnabled { get; set; }
        public TimeSpan QuietHoursStart { get; set; }
        public TimeSpan QuietHoursEnd { get; set; }
        public Dictionary<NotificationType, NotificationTypeSettings> TypeSettings { get; set; } = 
            new Dictionary<NotificationType, NotificationTypeSettings>();
    }

    public class NotificationTypeSettings
    {
        public bool Enabled { get; set; }
        public NotificationPriority Priority { get; set; }
        public NotificationDeliveryMethod DeliveryMethod { get; set; }
    }

    public class Notification
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // This enum should be defined in the Assignment model file
    // Adding it here for reference

}