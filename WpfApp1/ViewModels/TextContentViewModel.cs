using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class TextContentViewModel : NotifyPropertyChangedBase
    {
        private bool _isLoading;
        private readonly Content _content;

        public TextContentViewModel(Content content)
        {
            _content = content;
            CompleteCommand = new RelayCommand(Complete);
        }

        public Content Content => _content;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand CompleteCommand { get; }

        public bool? DialogResult { get; set; }

        private void Complete()
        {
            DialogResult = true;
            if (Window != null)
            {
                Window.DialogResult = DialogResult;
                Window.Close();
            }
        }

        private Window Window => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
    }
}
