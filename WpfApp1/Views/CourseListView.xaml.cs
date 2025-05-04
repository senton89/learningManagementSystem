using System;
using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class CourseListView : UserControl
    {
        public CourseListView()
        {
            InitializeComponent();
        }
        
        private void OnFilterSelected(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is CourseManagementViewModel viewModel && 
                sender is ComboBox comboBox && 
                comboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Tag is string filterName)
            {
                if (Enum.TryParse<CourseFilter>(filterName, out var filter))
                {
                    viewModel.CurrentFilter = filter;
                    viewModel.RefreshCommand.Execute(null);
                }
            }
        }
    }
}