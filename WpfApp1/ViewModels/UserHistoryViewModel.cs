using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// ViewModel для отображения истории изменений пользователя
    /// </summary>
    public class UserHistoryViewModel : NotifyPropertyChangedBase
    {
        private readonly AuditService _auditService;
        private readonly User _user;
        private ObservableCollection<AuditLog> _roleHistory;
        private ObservableCollection<AuditLog> _permissionHistory;
        private bool _isLoading;
        private string _errorMessage;

        public UserHistoryViewModel(AuditService auditService, User user)
        {
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _user = user ?? throw new ArgumentNullException(nameof(user));
            RoleHistory = new ObservableCollection<AuditLog>();
            PermissionHistory = new ObservableCollection<AuditLog>();
            
            LoadHistoryCommand = new RelayCommand(async () => await LoadHistoryAsync(), () => !IsLoading);
        }

        /// <summary>
        /// История изменений ролей пользователя
        /// </summary>
        public ObservableCollection<AuditLog> RoleHistory
        {
            get => _roleHistory;
            set => SetProperty(ref _roleHistory, value);
        }

        /// <summary>
        /// История изменений разрешений пользователя
        /// </summary>
        public ObservableCollection<AuditLog> PermissionHistory
        {
            get => _permissionHistory;
            set => SetProperty(ref _permissionHistory, value);
        }

        /// <summary>
        /// Флаг загрузки данных
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Команда загрузки истории
        /// </summary>
        public RelayCommand LoadHistoryCommand { get; }

        /// <summary>
        /// Загружает историю изменений пользователя
        /// </summary>
        private async Task LoadHistoryAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Загрузка истории изменений ролей
                var roleHistory = await _auditService.GetUserRoleHistoryAsync(_user.Id);
                RoleHistory.Clear();
                foreach (var log in roleHistory)
                {
                    RoleHistory.Add(log);
                }

                // Загрузка истории изменений разрешений
                var permissionHistory = await _auditService.GetUserPermissionHistoryAsync(_user.Id);
                PermissionHistory.Clear();
                foreach (var log in permissionHistory)
                {
                    PermissionHistory.Add(log);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при загрузке истории: {ex.Message}";
                await _auditService.LogActionAsync("System", "LoadHistoryError", $"Ошибка при загрузке истории для пользователя {_user.Id}: {ex.Message}");
                MessageBox.Show($"Не удалось загрузить историю изменений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
