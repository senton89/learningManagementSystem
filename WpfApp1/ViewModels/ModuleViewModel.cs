using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class ModuleViewModel : INotifyPropertyChanged
    {
        private int _id;
        private string _title;
        private string _description;
        private int _orderIndex;
        private int _courseId;
        private bool _isExpanded;

        public ObservableCollection<Content> Contents { get; } = new ObservableCollection<Content>();
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public int OrderIndex
        {
            get => _orderIndex;
            set
            {
                if (_orderIndex != value)
                {
                    _orderIndex = value;
                    OnPropertyChanged(nameof(OrderIndex));
                }
            }
        }

        public int CourseId
        {
            get => _courseId;
            set
            {
                if (_courseId != value)
                {
                    _courseId = value;
                    OnPropertyChanged(nameof(CourseId));
                }
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public ModuleViewModel(Module module)
        {
            Id = module.Id;
            Title = module.Title;
            Description = module.Description;
            OrderIndex = module.OrderIndex;
            CourseId = module.CourseId;
            IsExpanded = false;
        
            if (module.Contents != null)
            {
                foreach (var content in module.Contents.OrderBy(c => c.OrderIndex))
                {
                    // Убедимся, что у контента установлена связь с модулем
                    content.ModuleId = module.Id;
                    Contents.Add(content);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
