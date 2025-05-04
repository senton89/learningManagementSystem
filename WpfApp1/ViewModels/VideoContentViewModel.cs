using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApp1.Infrastructure;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class VideoContentViewModel : NotifyPropertyChangedBase
    {
        private readonly Content _content;
        private MediaElement _videoPlayer;
        private readonly DispatcherTimer _timer;
        private bool _isLoading;
        private bool _isPlaying;
        private double _position;
        private double _duration;
        private TimeSpan _currentTime;
        private TimeSpan _totalTime;
        private bool _canComplete;

        public VideoContentViewModel(Content content, MediaElement videoPlayer)
        {
            _content = content;
            _videoPlayer = videoPlayer;
            
            // Настраиваем таймер для обновления позиции
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;

            // Команды
            PlayPauseCommand = new RelayCommand(PlayPause);
            RewindCommand = new RelayCommand(Rewind);
            ForwardCommand = new RelayCommand(Forward);
            CompleteCommand = new RelayCommand(Complete, () => CanComplete);

            // Загружаем видео
            VideoSource = new Uri(_content.Data);
        }

        public Content Content => _content;

        public Uri VideoSource { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (SetProperty(ref _isPlaying, value))
                {
                    OnPropertyChanged(nameof(PlayPauseIcon));
                }
            }
        }

        public double Position
        {
            get => _position;
            set
            {
                if (SetProperty(ref _position, value))
                {
                    _videoPlayer.Position = TimeSpan.FromSeconds(value);
                    CurrentTime = _videoPlayer.Position;
                    UpdateCanComplete();
                }
            }
        }

        public double Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        public TimeSpan CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public TimeSpan TotalTime
        {
            get => _totalTime;
            set => SetProperty(ref _totalTime, value);
        }

        public bool CanComplete
        {
            get => _canComplete;
            set
            {
                if (SetProperty(ref _canComplete, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string PlayPauseIcon => IsPlaying
            ? "M14,19H18V5H14M6,19H10V5H6V19Z" // Пауза
            : "M8,5.14V19.14L19,12.14L8,5.14Z"; // Плей

        public ICommand PlayPauseCommand { get; }
        public ICommand RewindCommand { get; }
        public ICommand ForwardCommand { get; }
        public ICommand CompleteCommand { get; }

        public void OnMediaOpened()
        {
            Duration = _videoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            TotalTime = _videoPlayer.NaturalDuration.TimeSpan;
            Play();
        }

        public void OnMediaEnded()
        {
            IsPlaying = false;
            _timer.Stop();
            UpdateCanComplete();
        }

        private void PlayPause()
        {
            if (IsPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void Play()
        {
            _videoPlayer.Play();
            IsPlaying = true;
            _timer.Start();
        }

        private void Pause()
        {
            _videoPlayer.Pause();
            IsPlaying = false;
            _timer.Stop();
        }

        private void Rewind()
        {
            Position = Math.Max(0, Position - 10);
        }

        private void Forward()
        {
            Position = Math.Min(Duration, Position + 10);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsPlaying) return;

            Position = _videoPlayer.Position.TotalSeconds;
            CurrentTime = _videoPlayer.Position;
        }

        private void UpdateCanComplete()
        {
            // Можно отметить как просмотренное, если просмотрено хотя бы 90% видео
            CanComplete = Position >= Duration * 0.9;
        }

        private void Complete()
        {
            DialogResult = true;
            if (Window != null)
            {
                Window.DialogResult = DialogResult;
                Window.Close();
            }
        }
        
        public void InitializePlayer(MediaElement player)
        {
            _videoPlayer = player;
    
            // Установим источник видео
            if (_videoPlayer != null && VideoSource != null)
            {
                _videoPlayer.Source = VideoSource;
            }
        }

        public bool? DialogResult { get; set; }

        private Window Window => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);

        public void Cleanup()
        {
            _timer.Stop();
            _videoPlayer.Stop();
            _videoPlayer.Close();
        }
    }
}
