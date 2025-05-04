using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class NotificationSettingsDialog : Window
    {
        public NotificationSettingsDialog(NotificationSettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Закрываем окно при успешном сохранении
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NotificationSettingsViewModel.HasChanges) &&
                    !viewModel.HasChanges)
                {
                    DialogResult = true;
                }
            };
        }
    }
}
