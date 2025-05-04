using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WpfApp1.Controls
{
    public partial class TimelineControl : UserControl
    {
        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register(
                nameof(StartDate),
                typeof(DateTime),
                typeof(TimelineControl),
                new PropertyMetadata(DateTime.Now, OnTimelinePropertyChanged));

        public static readonly DependencyProperty EndDateProperty =
            DependencyProperty.Register(
                nameof(EndDate),
                typeof(DateTime),
                typeof(TimelineControl),
                new PropertyMetadata(DateTime.Now.AddDays(7), OnTimelinePropertyChanged));

        public static readonly DependencyProperty CurrentDateProperty =
            DependencyProperty.Register(
                nameof(CurrentDate),
                typeof(DateTime),
                typeof(TimelineControl),
                new PropertyMetadata(DateTime.Now, OnTimelinePropertyChanged));

        public static readonly DependencyProperty MilestonesProperty =
            DependencyProperty.Register(
                nameof(Milestones),
                typeof(List<TimelineMilestone>),
                typeof(TimelineControl),
                new PropertyMetadata(new List<TimelineMilestone>(), OnTimelinePropertyChanged));

        public DateTime StartDate
        {
            get => (DateTime)GetValue(StartDateProperty);
            set => SetValue(StartDateProperty, value);
        }

        public DateTime EndDate
        {
            get => (DateTime)GetValue(EndDateProperty);
            set => SetValue(EndDateProperty, value);
        }

        public DateTime CurrentDate
        {
            get => (DateTime)GetValue(CurrentDateProperty);
            set => SetValue(CurrentDateProperty, value);
        }

        public List<TimelineMilestone> Milestones
        {
            get => (List<TimelineMilestone>)GetValue(MilestonesProperty);
            set => SetValue(MilestonesProperty, value);
        }

        private static void OnTimelinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimelineControl timeline)
            {
                timeline.UpdateTimeline();
            }
        }

        public TimelineControl()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateTimeline();
            SizeChanged += (s, e) => UpdateTimeline();
        }

        private void UpdateTimeline()
        {
            TimelineCanvas.Children.Clear();
            LabelsCanvas.Children.Clear();

            var totalDuration = (EndDate - StartDate).TotalMinutes;
            if (totalDuration <= 0) return;

            // Добавляем фон прогресса
            var progressWidth = Math.Max(0, Math.Min(ActualWidth,
                ActualWidth * (CurrentDate - StartDate).TotalMinutes / totalDuration));

            var progressRect = new Rectangle
            {
                Width = progressWidth,
                Height = 4,
                Fill = new SolidColorBrush(Color.FromRgb(0x81, 0xC7, 0x84))
            };
            TimelineCanvas.Children.Add(progressRect);
            Canvas.SetLeft(progressRect, 0);

            // Добавляем маркер текущего времени
            var currentMarker = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(0, 0),
                    new Point(8, 0),
                    new Point(4, 8)
                },
                Fill = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
            };
            TimelineCanvas.Children.Add(currentMarker);
            Canvas.SetLeft(currentMarker, progressWidth - 4);
            Canvas.SetTop(currentMarker, -4);

            // Добавляем метки времени
            var timePoints = GetTimePoints();
            foreach (var point in timePoints)
            {
                var position = ActualWidth * (point - StartDate).TotalMinutes / totalDuration;

                // Вертикальная линия
                var line = new Line
                {
                    X1 = position,
                    Y1 = 0,
                    X2 = position,
                    Y2 = 4,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xBD, 0xBD, 0xBD)),
                    StrokeThickness = 1
                };
                TimelineCanvas.Children.Add(line);

                // Метка времени
                var label = new TextBlock
                {
                    Text = GetTimeLabel(point),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75))
                };
                LabelsCanvas.Children.Add(label);
                Canvas.SetLeft(label, position - label.ActualWidth / 2);
            }

            // Добавляем вехи
            foreach (var milestone in Milestones)
            {
                var position = ActualWidth * (milestone.Date - StartDate).TotalMinutes / totalDuration;

                var marker = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(GetMilestoneColor(milestone.Type)),
                    ToolTip = milestone.Description
                };
                TimelineCanvas.Children.Add(marker);
                Canvas.SetLeft(marker, position - 4);
                Canvas.SetTop(marker, -2);

                // Анимация пульсации для важных вех
                if (milestone.Type == MilestoneType.Important)
                {
                    var animation = new DoubleAnimation
                    {
                        From = 0.7,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(1),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    marker.BeginAnimation(OpacityProperty, animation);
                }
            }
        }

        private List<DateTime> GetTimePoints()
        {
            var points = new List<DateTime>();
            var duration = EndDate - StartDate;

            if (duration.TotalDays <= 1)
            {
                // Для одного дня показываем часы
                var hour = StartDate.Hour;
                while (hour <= EndDate.Hour)
                {
                    points.Add(StartDate.Date.AddHours(hour));
                    hour += 3;
                }
            }
            else if (duration.TotalDays <= 7)
            {
                // Для недели показываем дни
                var date = StartDate.Date;
                while (date <= EndDate)
                {
                    points.Add(date);
                    date = date.AddDays(1);
                }
            }
            else
            {
                // Для большего периода показываем недели
                var date = StartDate.Date;
                while (date <= EndDate)
                {
                    points.Add(date);
                    date = date.AddDays(7);
                }
            }

            return points;
        }

        private string GetTimeLabel(DateTime date)
        {
            var duration = EndDate - StartDate;

            if (duration.TotalDays <= 1)
            {
                return date.ToString("HH:mm");
            }
            else if (duration.TotalDays <= 7)
            {
                return date.ToString("dd.MM");
            }
            else
            {
                return date.ToString("dd.MM.yy");
            }
        }

        private static Color GetMilestoneColor(MilestoneType type)
        {
            return type switch
            {
                MilestoneType.Start => Color.FromRgb(0x4C, 0xAF, 0x50),      // Зеленый
                MilestoneType.End => Color.FromRgb(0xF4, 0x43, 0x36),        // Красный
                MilestoneType.Important => Color.FromRgb(0xFF, 0xB7, 0x4D),  // Оранжевый
                MilestoneType.Regular => Color.FromRgb(0x64, 0xB5, 0xF6),    // Синий
                _ => Color.FromRgb(0xBD, 0xBD, 0xBD)                         // Серый
            };
        }
    }

    public class TimelineMilestone
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public MilestoneType Type { get; set; }
    }

    public enum MilestoneType
    {
        Start,
        End,
        Important,
        Regular
    }
}
