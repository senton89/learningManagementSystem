using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class CourseEditDialog : Window
    {
        public CourseEditDialog(CourseEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
