using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class RegistrationView : UserControl
    {
        public RegistrationView()
        {
            InitializeComponent();


            PasswordBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is RegistrationViewModel vm)
                    vm.Password = PasswordBox.Password;
            };

            ConfirmPasswordBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is RegistrationViewModel vm)
                    vm.ConfirmPassword = ConfirmPasswordBox.Password;
            };
        }
    }
}
