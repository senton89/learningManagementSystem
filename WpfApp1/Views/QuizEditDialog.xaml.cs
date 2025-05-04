using System.Windows;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class QuizEditDialog : Window
    {
        public QuizEditDialog(Quiz quiz, CourseService courseService = null, Content content = null)
        {
            InitializeComponent();
            DataContext = new QuizEditViewModel(quiz, courseService, content);
            ((QuizEditViewModel)DataContext).RequestClose += (s, e) => 
            {
                DialogResult = true;
                Close();
            };
        }
    }
}
