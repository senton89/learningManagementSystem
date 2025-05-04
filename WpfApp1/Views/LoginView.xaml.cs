using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            
            PasswordBox.PasswordChanged += (s, e) => {
                if (DataContext is LoginViewModel viewModel)
                    viewModel.Password = PasswordBox.Password;
            };
        }
    }
}
