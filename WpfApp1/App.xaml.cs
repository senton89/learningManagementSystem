using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WpfApp1.Infrastructure;
using WpfApp1.Services;
using WpfApp1.ViewModels;

namespace WpfApp1;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        // Set up dependency injection
        var services = new ServiceCollection();

        var dir = AppDomain.CurrentDomain.BaseDirectory.Remove(
            AppDomain.CurrentDomain.BaseDirectory.LastIndexOf("bin", StringComparison.Ordinal));
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(dir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add DbContext
        services.AddDbContext<LmsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddSingleton<IDbContextFactory<LmsDbContext>>(provider => 
            new DbContextFactory<LmsDbContext>(() => 
                new LmsDbContext(new DbContextOptionsBuilder<LmsDbContext>()
                    .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                    .Options)));
        
        // Add services
        services.AddScoped<AuditService>(provider => 
        {
            var dbContextFactory = provider.GetService<IDbContextFactory<LmsDbContext>>();
            if (dbContextFactory != null)
            {
                return new AuditService(dbContextFactory);
            }
            else
            {
                var dbContext = provider.GetService<LmsDbContext>();
                return new AuditService(dbContext);
            }
        });
        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<UserService>();
        services.AddSingleton<CourseService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<FileStorageService>(provider => 
            new FileStorageService(
                configuration["Storage:AssignmentFiles"] ?? "Files/Assignments",
                provider.GetRequiredService<CourseService>(),
                provider.GetRequiredService<UserService>()
            ));
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
    
        try
        {
            await DbInitializer.SeedDatabaseAsync(serviceProvider);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing database: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        // Create and show main window
        var mainWindow = new MainWindow(serviceProvider);
        if (mainWindow.DataContext is MainWindowViewModel viewModel)
        {
            // Start with login view
            viewModel.NavigateToLoginCommand.Execute(null);
        }
    
        // Now show the window
        mainWindow.Show();
    }
    
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "UI Thread Exception");
        MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nPlease restart the application.", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LogException(exception, "Non-UI Thread Exception");
        MessageBox.Show($"A critical error occurred: {exception?.Message}\n\nThe application will now close.", 
            "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void LogException(Exception exception, string source)
    {
        // Simple file logging for now
        try
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logPath);
            string logFile = Path.Combine(logPath, $"error_{DateTime.Now:yyyyMMdd}.log");
            
            using (var writer = File.AppendText(logFile))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}");
                writer.WriteLine($"Message: {exception.Message}");
                writer.WriteLine($"StackTrace: {exception.StackTrace}");
                writer.WriteLine(new string('-', 80));
            }
        }
        catch
        {
            // If logging fails, we can't do much
        }
    }
}