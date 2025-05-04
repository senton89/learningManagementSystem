using System.Windows;
using WpfApp1.Models;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class ModuleEditDialog : Window
    {
        public Module Module { get; private set; }

        public ModuleEditDialog(Module module)
        {
            InitializeComponent();
            Module = module;
        
            var viewModel = new ModuleEditViewModel(module);
            DataContext = viewModel;
        
            viewModel.RequestClose += (s, e) =>
            {
                DialogResult = true;
                Close();
            };
        }
    }
}
