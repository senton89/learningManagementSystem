using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WpfApp1.Infrastructure;
using WpfApp1.Services;
using WpfApp1.ViewModels;

namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(ServiceProvider serviceProvider)
    {
        InitializeComponent();

        try
        {
            var dbContext = serviceProvider.GetRequiredService<LmsDbContext>();
            var courseService = serviceProvider.GetRequiredService<CourseService>();
            var userService = serviceProvider.GetRequiredService<UserService>();
            var notificationService = serviceProvider.GetRequiredService<NotificationService>();

            var viewModel = new MainWindowViewModel(dbContext, courseService, userService, notificationService, serviceProvider.GetRequiredService<IDbContextFactory<LmsDbContext>>());
            DataContext = viewModel;
        
            // Ensure we start with login view
            viewModel.NavigateToLoginCommand.Execute(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}