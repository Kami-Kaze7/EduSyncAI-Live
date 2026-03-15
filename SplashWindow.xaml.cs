using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EduSyncAI
{
    public partial class SplashWindow : Window
    {
        private int _stepCount = 0;
        private const int TotalSteps = 3; // WebAPI, Next.js, Python

        public SplashWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Update the splash screen with a new status message.
        /// </summary>
        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentStatus.Text = message;

                // Determine color based on message content
                var color = message.Contains("✓") ? "#22c55e"  // green for success
                          : message.Contains("⚠") ? "#f59e0b"  // yellow for warning
                          : "#a5b4fc";                          // light blue for info

                if (message.Contains("✓"))
                {
                    _stepCount++;
                    ProgressBar.Value = (double)_stepCount / TotalSteps * 100;
                }

                var tb = new TextBlock
                {
                    Text = message,
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    Margin = new Thickness(0, 2, 0, 2),
                };
                StatusPanel.Children.Add(tb);
            });
        }

        /// <summary>
        /// Show an error message on the splash screen.
        /// </summary>
        public void ShowError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var tb = new TextBlock
                {
                    Text = "⚠ " + message,
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = TextWrapping.Wrap,
                };
                StatusPanel.Children.Add(tb);
                CurrentStatus.Text = "Warning: Some services may not be available.";
            });
        }

        /// <summary>
        /// Mark startup as complete and close the splash.
        /// </summary>
        public void MarkComplete()
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = 100;
                CurrentStatus.Text = "Ready! Launching EduSync AI...";
            });
        }
    }
}
