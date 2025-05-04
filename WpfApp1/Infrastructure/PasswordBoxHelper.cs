using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Infrastructure
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject d)
        {
            return (string)d.GetValue(BoundPasswordProperty);
        }

        public static void SetBoundPassword(DependencyObject d, string value)
        {
            d.SetValue(BoundPasswordProperty, value);
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = d as PasswordBox;
            if (passwordBox != null)
            {
                // Detach event handler to avoid recursion
                passwordBox.PasswordChanged -= PasswordChanged;

                if (e.NewValue != null && passwordBox.Password != e.NewValue.ToString())
                {
                    passwordBox.Password = e.NewValue.ToString();
                }
            
                // Reattach event handler
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                var boundPassword = GetBoundPassword(passwordBox);
                if (passwordBox.Password != boundPassword)
                {
                    SetBoundPassword(passwordBox, passwordBox.Password);
                }
            }
        }
    }
}