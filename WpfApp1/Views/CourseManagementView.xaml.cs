using System.Windows;
using WpfApp1.Infrastructure;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class CourseManagementView : Window
    {
        public CourseManagementView(CourseService courseService, UserService userService, User currentUser)
        {
            InitializeComponent();
            DataContext = new CourseManagementViewModel(courseService, userService, currentUser);
        }
    }
}
