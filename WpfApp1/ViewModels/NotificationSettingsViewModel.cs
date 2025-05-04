using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfApp1.Models;
using WpfApp1.Services;
using NotificationDeliveryMethod = WpfApp1.Models.NotificationDeliveryMethod;
using NotificationPriority = WpfApp1.Models.NotificationPriority;
using NotificationSettings = WpfApp1.Models.NotificationSettings;
using NotificationType = WpfApp1.Models.NotificationType;
using NotificationTypeSettings = WpfApp1.Models.NotificationTypeSettings;

namespace WpfApp1.ViewModels
{
    public partial class NotificationSettingsViewModel : ObservableObject
    {
        private readonly NotificationService _notificationService;
        private readonly CourseService _courseService;
        private readonly UserService _userService;

        [ObservableProperty]
        private bool _notificationsEnabled;

        [ObservableProperty]
        private bool _soundEnabled;

        [ObservableProperty]
        private bool _quietHoursEnabled;

        [ObservableProperty]
        private TimeSpan _quietHoursStart;

        [ObservableProperty]
        private TimeSpan _quietHoursEnd;

        [ObservableProperty]
        private ObservableCollection<NotificationTypeSettingsViewModel> _typeSettings;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private bool _hasChanges;

        public NotificationSettingsViewModel(
            NotificationService notificationService,
            CourseService courseService,
            UserService userService)
        {
            _notificationService = notificationService;
            _courseService = courseService;
            _userService = userService;

            TypeSettings = new ObservableCollection<NotificationTypeSettingsViewModel>();

            // Команды
            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            ResetToDefaultCommand = new AsyncRelayCommand(ResetToDefaultAsync);
            TestNotificationCommand = new AsyncRelayCommand(TestNotificationAsync);

            // Загружаем настройки
            LoadSettingsAsync();
        }

        private async void LoadSettingsAsync()
        {
            try
            {
                var settings = await _courseService.GetNotificationSettingsAsync(_userService.CurrentUser.Id);
                if (settings == null)
                {
                    settings = CreateDefaultSettings();
                }

                UpdateFromSettings(settings);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при загрузке настроек: {ex.Message}";
            }
        }

        private void UpdateFromSettings(Models.NotificationSettings settings)
        {
            NotificationsEnabled = settings.NotificationsEnabled;
            SoundEnabled = settings.SoundEnabled;
            QuietHoursEnabled = settings.QuietHoursEnabled;
            QuietHoursStart = settings.QuietHoursStart;
            QuietHoursEnd = settings.QuietHoursEnd;

            TypeSettings.Clear();
            foreach (var type in Enum.GetValues(typeof(Models.NotificationType)).Cast<Models.NotificationType>())
            {
                var typeSettings = settings.TypeSettings.TryGetValue(type, out var value)
                    ? value
                    : new NotificationTypeSettings
                    {
                        Enabled = true,
                        Priority = Models.NotificationPriority.Normal,
                        DeliveryMethod = Models.NotificationDeliveryMethod.Toast
                    };

                TypeSettings.Add(new NotificationTypeSettingsViewModel(type, typeSettings));
            }

            HasChanges = false;
        }

        private Models.NotificationSettings CreateDefaultSettings()
        {
            return new Models.NotificationSettings
            {
                UserId = _userService.CurrentUser.Id,
                NotificationsEnabled = true,
                SoundEnabled = true,
                QuietHoursEnabled = true,
                QuietHoursStart = new TimeSpan(23, 0, 0), // 23:00
                QuietHoursEnd = new TimeSpan(7, 0, 0),    // 07:00
                TypeSettings = Enum.GetValues(typeof(NotificationType))
                    .Cast<NotificationType>()
                    .ToDictionary(
                        type => type,
                        type => new NotificationTypeSettings
                        {
                            Enabled = true,
                            Priority = NotificationPriority.Normal,
                            DeliveryMethod = NotificationDeliveryMethod.Toast
                        })
            };
        }

        private bool CanSave()
        {
            return HasChanges && !IsSaving;
        }

        private async Task SaveAsync()
        {
            try
            {
                IsSaving = true;
                StatusMessage = "Сохранение настроек...";

                var settings = new Models.NotificationSettings
                {
                    UserId = _userService.CurrentUser.Id,
                    NotificationsEnabled = NotificationsEnabled,
                    SoundEnabled = SoundEnabled,
                    QuietHoursEnabled = QuietHoursEnabled,
                    QuietHoursStart = QuietHoursStart,
                    QuietHoursEnd = QuietHoursEnd,
                    TypeSettings = TypeSettings.ToDictionary(
                        vm => vm.Type,
                        vm => new NotificationTypeSettings
                        {
                            Enabled = vm.IsEnabled,
                            Priority = vm.Priority,
                            DeliveryMethod = vm.DeliveryMethod
                        })
                };

                await _notificationService.UpdateSettingsAsync(ConvertToServiceSettings(settings));

                HasChanges = false;
                StatusMessage = "Настройки сохранены";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при сохранении настроек: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }
        
        
        private Services.NotificationSettings ConvertToServiceSettings(Models.NotificationSettings settings)
        {
            // Create a mapping between the two types
            var serviceSettings = new Services.NotificationSettings
            {
                UserId = settings.UserId,
                NotificationsEnabled = settings.NotificationsEnabled,
                SoundEnabled = settings.SoundEnabled,
                QuietHoursEnabled = settings.QuietHoursEnabled,
                QuietHoursStart = settings.QuietHoursStart,
                QuietHoursEnd = settings.QuietHoursEnd,
                TypeSettings = new Dictionary<Services.NotificationType, Services.NotificationTypeSettings>()
            };

            // Map notification types
            foreach (var kvp in settings.TypeSettings)
            {
                if (TryMapNotificationType(kvp.Key, out var serviceType))
                {
                    serviceSettings.TypeSettings[serviceType] = new Services.NotificationTypeSettings
                    {
                        Enabled = kvp.Value.Enabled,
                        Priority = (Services.NotificationPriority)(int)kvp.Value.Priority,
                        DeliveryMethod = (Services.NotificationDeliveryMethod)(int)kvp.Value.DeliveryMethod
                    };
                }
            }

            return serviceSettings;
        }

        private bool TryMapNotificationType(Models.NotificationType modelType, out Services.NotificationType serviceType)
        {
            // Map between the two enum types
            switch (modelType)
            {
                case Models.NotificationType.DeadlineApproaching:
                    serviceType = Services.NotificationType.DeadlineApproaching;
                    return true;
                case Models.NotificationType.DeadlineMissed:
                    serviceType = Services.NotificationType.DeadlineMissed;
                    return true;
                case Models.NotificationType.AssignmentGraded:
                    serviceType = Services.NotificationType.AssignmentGraded;
                    return true;
                // Add other mappings as needed
                default:
                    serviceType = Services.NotificationType.System;
                    return false;
            }
        }

        private async Task ResetToDefaultAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите сбросить все настройки уведомлений к значениям по умолчанию?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var settings = CreateDefaultSettings();
                    UpdateFromSettings(settings);
                    await SaveAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при сбросе настроек: {ex.Message}";
            }
        }

        private async Task TestNotificationAsync()
        {
            try
            {
                StatusMessage = "Отправка тестового уведомления...";
                
                var testNotification = new Services.Notification
                {
                    Title = "Тестовое уведомление",
                    Message = "Это тестовое уведомление для проверки настроек",
                    Type = Services.NotificationType.System,
                    Priority = Services.NotificationPriority.Normal,
                    Timestamp = DateTime.Now
                };

                await _notificationService.SendNotificationAsync(testNotification);
                StatusMessage = "Тестовое уведомление отправлено";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при отправке тестового уведомления: {ex.Message}";
            }
        }

        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand ResetToDefaultCommand { get; }
        public IAsyncRelayCommand TestNotificationCommand { get; }

        partial void OnNotificationsEnabledChanged(bool value) => HasChanges = true;
        partial void OnSoundEnabledChanged(bool value) => HasChanges = true;
        partial void OnQuietHoursEnabledChanged(bool value) => HasChanges = true;
        partial void OnQuietHoursStartChanged(TimeSpan value) => HasChanges = true;
        partial void OnQuietHoursEndChanged(TimeSpan value) => HasChanges = true;
    }

    public partial class NotificationTypeSettingsViewModel : ObservableObject
    {
        private readonly NotificationType _type;
        private readonly Action _onChanged;

        public NotificationType Type => _type;

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private NotificationPriority _priority;

        [ObservableProperty]
        private NotificationDeliveryMethod _deliveryMethod;

        public string DisplayName => GetDisplayName();
        public string Description => GetDescription();

        public NotificationTypeSettingsViewModel(NotificationType type, NotificationTypeSettings settings)
        {
            _type = type;
            _isEnabled = settings.Enabled;
            _priority = settings.Priority;
            _deliveryMethod = settings.DeliveryMethod;
        }

        private string GetDisplayName()
        {
            return _type switch
            {
                Models.NotificationType.DeadlineApproaching => "Approaching Deadlines",
                Models.NotificationType.DeadlineMissed => "Missed Deadlines",
                Models.NotificationType.AssignmentGraded => "Graded Assignments",
                Models.NotificationType.AssignmentCreated => "New Assignments",
                Models.NotificationType.AssignmentModified => "Modified Assignments",
                Models.NotificationType.AssignmentDeleted => "Deleted Assignments",
                Models.NotificationType.CommentAdded => "New Comments",
                Models.NotificationType.GradeChanged => "Grade Changes",
                _ => _type.ToString()
            };
        }

        private string GetDescription()
        {
            return _type switch
            {
                Models.NotificationType.DeadlineApproaching => "Notifications about approaching assignment deadlines",
                Models.NotificationType.DeadlineMissed => "Notifications about missed deadlines",
                Models.NotificationType.AssignmentGraded => "Notifications about graded assignments",
                Models.NotificationType.AssignmentCreated => "Notifications about new assignments",
                Models.NotificationType.AssignmentModified => "Notifications about modified assignments",
                Models.NotificationType.AssignmentDeleted => "Notifications about deleted assignments",
                Models.NotificationType.CommentAdded => "Notifications about new comments",
                Models.NotificationType.GradeChanged => "Notifications about grade changes",
                _ => string.Empty
            };
        }

        partial void OnIsEnabledChanged(bool value) => _onChanged?.Invoke();
        partial void OnPriorityChanged(NotificationPriority value) => _onChanged?.Invoke();
        partial void OnDeliveryMethodChanged(NotificationDeliveryMethod value) => _onChanged?.Invoke();
    }
}
