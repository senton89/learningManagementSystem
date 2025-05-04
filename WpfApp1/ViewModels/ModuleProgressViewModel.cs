using System.Collections.ObjectModel;
using System.Linq;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class ModuleProgressViewModel : NotifyPropertyChangedBase
    {
        private readonly Module _module;
        private double _progress;

        public Module Module => _module;
        public ObservableCollection<ContentProgressViewModel> ContentProgress { get; } = new();

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public ModuleProgressViewModel(Module module)
        {
            _module = module;
        }

        public void UpdateProgress()
        {
            var totalContent = ContentProgress.Count;
            var completedContent = ContentProgress.Count(c => c.Status == ProgressStatus.Completed);
            Progress = totalContent > 0 ? (double)completedContent / totalContent * 100 : 0;
        }
    }
}