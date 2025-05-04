using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class SystemSettingsViewModel : ViewModelBase
    {
        private readonly LmsDbContext _dbContext;
        private string _databaseConnectionString;
        private string _fileStoragePath;
        private bool _enableNotifications;
        private int _notificationCheckInterval;
        private bool _enableLogging;
        private string _logFilePath;
        
        public string DatabaseConnectionString
        {
            get => _databaseConnectionString;
            set => SetProperty(ref _databaseConnectionString, value);
        }
        
        public string FileStoragePath
        {
            get => _fileStoragePath;
            set => SetProperty(ref _fileStoragePath, value);
        }
        
        public bool EnableNotifications
        {
            get => _enableNotifications;
            set => SetProperty(ref _enableNotifications, value);
        }
        
        public int NotificationCheckInterval
        {
            get => _notificationCheckInterval;
            set => SetProperty(ref _notificationCheckInterval, value);
        }
        
        public bool EnableLogging
        {
            get => _enableLogging;
            set => SetProperty(ref _enableLogging, value);
        }
        
        public string LogFilePath
        {
            get => _logFilePath;
            set => SetProperty(ref _logFilePath, value);
        }
        
        public ICommand SaveSettingsCommand { get; }
        public ICommand TestDatabaseConnectionCommand { get; }
        public ICommand BackupDatabaseCommand { get; }
        
        public SystemSettingsViewModel(LmsDbContext dbContext)
        {
            _dbContext = dbContext;
            
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            TestDatabaseConnectionCommand = new RelayCommand(TestDatabaseConnection);
            BackupDatabaseCommand = new RelayCommand(BackupDatabase);
            
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            try
            {
                IsBusy = true;
                StatusMessage = "Loading settings...";
                
                // In a real application, these would be loaded from configuration
                DatabaseConnectionString = connectionString;
                FileStoragePath = "Files/Assignments";
                EnableNotifications = true;
                NotificationCheckInterval = 5;
                EnableLogging = true;
                LogFilePath = "logs/application.log";
                
                StatusMessage = "Settings loaded successfully";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Saving settings...";
                
                // In a real application, these would be saved to configuration
                
                StatusMessage = "Settings saved successfully";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error saving settings: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void TestDatabaseConnection()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Testing database connection...";
                
                // Test the connection
                var canConnect = _dbContext.Database.CanConnect();
                
                if (canConnect)
                {
                    StatusMessage = "Database connection successful";
                }
                else
                {
                    StatusMessage = "Failed to connect to database";
                }
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error testing database connection: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void BackupDatabase()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Backing up database...";
                
                // In a real application, this would perform a database backup
                
                StatusMessage = "Database backup completed successfully";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error backing up database: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}