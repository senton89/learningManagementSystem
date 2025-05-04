using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views;

public partial class AssignmentDialog : Window  
{
    public AssignmentDialog()
    {
        InitializeComponent();
    }
    
    private void AssignmentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AssignmentViewModel viewModel)
        {
            viewModel.Initialize();
        }
    }
}