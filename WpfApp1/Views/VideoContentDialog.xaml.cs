using System.Windows;
using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class VideoContentDialog : Window
    {
        public VideoContentDialog()
        {
            InitializeComponent();
            
            // Clean up resources when window is closed
            Closed += (s, e) =>
            {
                if (DataContext is VideoContentViewModel viewModel)
                {
                    viewModel.Cleanup();
                }
            };
        }
        
        private void VideoContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is VideoContentViewModel viewModel)
            {
                // Убедимся, что MediaElement инициализирован
                viewModel.InitializePlayer(VideoPlayer);
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (DataContext is VideoContentViewModel viewModel)
            {
                viewModel.OnMediaOpened();
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (DataContext is VideoContentViewModel viewModel)
            {
                viewModel.OnMediaEnded();
            }
        }
    }
}