using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class ContentProgressViewModel : NotifyPropertyChangedBase
    {
        private readonly StudentProgress _progress;

        public Content Content => _progress.Content;
        public ProgressStatus Status => _progress.Status;
        public bool CanStart => Status != ProgressStatus.Completed;

        public ContentProgressViewModel(StudentProgress progress)
        {
            _progress = progress;
        }
    }
}