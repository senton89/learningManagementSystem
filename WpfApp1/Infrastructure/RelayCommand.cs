using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfApp1.Infrastructure
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        private readonly Func<Task> _executeAsync;
        private bool _isExecuting;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public RelayCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute());
        }

        public void Execute(object parameter)
        {
            if (_isExecuting)
                return;
                
            if (_executeAsync != null)
            {
                ExecuteAsync().ConfigureAwait(false);
                return;
            }
            
            _execute();
        }
        
        public async Task ExecuteAsync()
        {
            if (_isExecuting)
                return;
                
            try
            {
                _isExecuting = true;
                CommandManager.InvalidateRequerySuggested();
                
                if (_executeAsync != null)
                    await _executeAsync();
                else
                    _execute();
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;
        private readonly Func<T, Task> _executeAsync;
        private bool _isExecuting;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public RelayCommand(Func<T, Task> executeAsync, Predicate<T> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute((T)parameter));
        }

        public void Execute(object parameter)
        {
            if (_isExecuting)
                return;
                
            if (_executeAsync != null)
            {
                ExecuteAsync((T)parameter).ConfigureAwait(false);
                return;
            }
            
            _execute((T)parameter);
        }
        
        public async Task ExecuteAsync(T parameter)
        {
            if (_isExecuting)
                return;
                
            try
            {
                _isExecuting = true;
                CommandManager.InvalidateRequerySuggested();
                
                if (_executeAsync != null)
                    await _executeAsync(parameter);
                else
                    _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
