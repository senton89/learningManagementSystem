using System.Windows;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class ContentEditDialog : Window
    {
        public ContentEditDialog(Content content, int moduleId, CourseService courseService = null)
        {
            InitializeComponent();
            DataContext = new ContentEditViewModel(content, moduleId, courseService);
            ((ContentEditViewModel)DataContext).RequestClose += (s, e) => 
            {
                DialogResult = true;
                Close();
            };
        }
    }
}
