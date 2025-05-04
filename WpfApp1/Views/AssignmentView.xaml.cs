using System;
using System.Windows.Controls;
using WpfApp1.Controls;
using WpfApp1.Models;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class AssignmentView : UserControl
    {
        public AssignmentView()
        {
            InitializeComponent();
        }
        private void OnFileAdded(object sender, FileUploadEventArgs e)
        {
            if (DataContext is AssignmentViewModel viewModel)
            {
                viewModel.OnFileAdded(sender, new FileAddedEventArgs(new FileInfo
                {
                    FileName = e.File.FileName,
                    Size = e.File.TotalBytes,
                    ContentType = "application/octet-stream", // Default content type
                    UploadDate = DateTime.Now
                }));
            }
        }

        private void OnFileRemoved(object sender, FileUploadEventArgs e)
        {
            if (DataContext is AssignmentViewModel viewModel)
            {
                viewModel.OnFileRemoved(sender, new FileRemovedEventArgs(new FileInfo
                {
                    FileName = e.File.FileName
                }));
            }
        }
    }
}
