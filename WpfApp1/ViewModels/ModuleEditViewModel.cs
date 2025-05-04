using System;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class ModuleEditViewModel : NotifyPropertyChangedBase
    {
        private readonly Module _module;
        private string _title;
        private string _description;
        private int _orderIndex;

        public event EventHandler RequestClose;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public int OrderIndex
        {
            get => _orderIndex;
            set => SetProperty(ref _orderIndex, value);
        }

        public ICommand SaveCommand { get; }

        public ModuleEditViewModel(Module module)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _title = module.Title;
            _description = module.Description;
            _orderIndex = module.OrderIndex;

            SaveCommand = new RelayCommand(Save, CanSave);
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title);
        }

        private void Save()
        {
            _module.Title = Title;
            _module.Description = Description;
            _module.OrderIndex = OrderIndex;
            
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
