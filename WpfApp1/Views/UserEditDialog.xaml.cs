using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class UserEditDialog : Window
    {
        public UserEditDialog(UserEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            PasswordBox.PasswordChanged += (s, e) => {
                if (DataContext is UserEditViewModel viewModel)
                    viewModel.Password = PasswordBox.Password;
            };
            ConfirmPasswordBox.PasswordChanged += (s, e) => {
                if (DataContext is UserEditViewModel viewModel)
                    viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            };
            
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(viewModel.DialogResult) && viewModel.DialogResult.HasValue)
                {
                    this.DialogResult = viewModel.DialogResult;
                }
            };
        }
    }
}
