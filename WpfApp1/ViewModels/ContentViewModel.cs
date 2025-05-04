using System;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Views;

namespace WpfApp1.ViewModels;

public class ContentViewModel : ViewModelBase
{
    private readonly Content _content;
    private readonly CourseService _courseService;
    private readonly UserService _userService;

    public Content Content => _content;
    public string Title => _content.Title;
    public ContentType Type => _content.Type;
    public int OrderIndex => _content.OrderIndex;

    public bool IsTeacher => _userService?.CurrentUser?.Role == UserRole.Teacher ||
                             _userService?.CurrentUser?.Role == UserRole.Administrator;

    public ContentViewModel(Content content, CourseService courseService, UserService userService)
    {
        _content = content;
        _courseService = courseService;
        _userService = userService;
    }

    private async void AddContent()
    {
        if (!IsTeacher) return;

        try
        {
            // Create a new content for the same module
            var newContent = new Content
            {
                Title = "New Content",
                Type = ContentType.Text,
                Data = "Enter content here",
                ModuleId = _content.ModuleId
            };

            // Show content edit dialog
            var dialog = new ContentEditDialog(newContent, _content.ModuleId, _courseService);
            if (dialog.ShowDialog() == true)
            {
                // Save the content to the database
                await _courseService.AddContentAsync(_content.ModuleId, newContent, _userService.CurrentUser.Id);
                StatusMessage = "Content added successfully";

                // Notify the parent view model to refresh
                RefreshRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error adding content: " + ex.Message;
        }
    }

    // Add an event to notify parent view models when a refresh is needed
    public event EventHandler RefreshRequested;
}