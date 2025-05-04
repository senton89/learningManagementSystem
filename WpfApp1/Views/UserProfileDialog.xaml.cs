using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class UserProfileDialog : Window
    {
        public UserProfileDialog()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserProfileViewModel viewModel)
            {
                // Validate data
                if (string.IsNullOrWhiteSpace(viewModel.User.FirstName) ||
                    string.IsNullOrWhiteSpace(viewModel.User.LastName) ||
                    string.IsNullOrWhiteSpace(viewModel.User.Email))
                {
                    MessageBox.Show("Please fill in all required fields.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}