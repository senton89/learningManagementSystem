using System;
using System.Collections.Generic;

namespace WpfApp1.Models
{
    public class NotificationSettings
    {
        public int UserId { get; set; }
        public bool NotificationsEnabled { get; set; }
        public bool SoundEnabled { get; set; }
        public bool QuietHoursEnabled { get; set; }
        public TimeSpan QuietHoursStart { get; set; }
        public TimeSpan QuietHoursEnd { get; set; }
        public Dictionary<NotificationType, NotificationTypeSettings> TypeSettings { get; set; }

        public NotificationSettings()
        {
            TypeSettings = new Dictionary<NotificationType, NotificationTypeSettings>
            {
                [NotificationType.DeadlineApproaching] = new NotificationTypeSettings 
                { 
                    Enabled = true,
                    Priority = NotificationPriority.Normal,
                    DeliveryMethod = NotificationDeliveryMethod.All
                },
                [NotificationType.DeadlineMissed] = new NotificationTypeSettings 
                { 
                    Enabled = true,
                    Priority = NotificationPriority.High,
                    DeliveryMethod = NotificationDeliveryMethod.All
                },
                [NotificationType.AssignmentGraded] = new NotificationTypeSettings 
                { 
                    Enabled = true,
                    Priority = NotificationPriority.Normal,
                    DeliveryMethod = NotificationDeliveryMethod.All
                },
                [NotificationType.AssignmentCreated] = new NotificationTypeSettings 
                { 
                    Enabled = true,
                    Priority = NotificationPriority.Normal,
                    DeliveryMethod = NotificationDeliveryMethod.All
                },
                [NotificationType.AssignmentModified] = new NotificationTypeSettings 
                { 
                    Enabled = true,
                    Priority = NotificationPriority.Normal,
                    DeliveryMethod = NotificationDeliveryMethod.All
                },
                [NotificationType.AssignmentDeleted] = new NotificationTypeSettings 
                { 
                    Enabled = true,
                    Priority = NotificationPriority.Normal,
                    DeliveryMethod = NotificationDeliveryMethod.All
                },
                [NotificationType.CommentAdded] = new NotificationTypeSettings 
                { 
                    Enabled = true,
                    Priority = NotificationPriority.Normal,
                    DeliveryMethod = NotificationDeliveryMethod.All
                },
                [NotificationType.GradeChanged] = new NotificationTypeSettings 
                { 
                    Enabled = true,
                    Priority = NotificationPriority.Normal,
                    DeliveryMethod = NotificationDeliveryMethod.All
                }
            };
        }
    }

    public class NotificationTypeSettings
    {
        public bool Enabled { get; set; }
        public NotificationPriority Priority { get; set; }
        public NotificationDeliveryMethod DeliveryMethod { get; set; }
    }

    public enum NotificationType
    {
        DeadlineApproaching,
        DeadlineMissed,
        AssignmentGraded,
        AssignmentCreated,
        AssignmentModified,
        AssignmentDeleted,
        CommentAdded,
        GradeChanged
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
        Email = 2,
        SMS = 4,
        All = Toast | Email
    }
}
