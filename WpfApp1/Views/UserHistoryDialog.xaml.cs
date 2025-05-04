using System.Windows;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для UserHistoryDialog.xaml
    /// </summary>
    public partial class UserHistoryDialog : Window
    {
        private readonly UserHistoryViewModel _viewModel;

        public UserHistoryDialog(AuditService auditService, User user)
        {
            InitializeComponent();
            _viewModel = new UserHistoryViewModel(auditService, user);
            DataContext = _viewModel;
            
            Loaded += UserHistoryDialog_Loaded;
        }

        private async void UserHistoryDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadHistoryCommand.ExecuteAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
