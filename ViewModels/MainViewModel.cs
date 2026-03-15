using System;
using System.ComponentModel;
using System.Windows.Input;

namespace EduSyncAI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object? _currentView;
        private string? _currentViewName;

        public object? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        public string? CurrentViewName
        {
            get => _currentViewName;
            set
            {
                _currentViewName = value;
                OnPropertyChanged(nameof(CurrentViewName));
            }
        }

        public ICommand ShowSessionManagementCommand { get; }
        public ICommand ShowAttendanceCommand { get; }
        public ICommand LogoutCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            ShowSessionManagementCommand = new RelayCommand(ShowSessionManagement);
            ShowAttendanceCommand = new RelayCommand(ShowAttendance);
            LogoutCommand = new RelayCommand(Logout);

            // Default view
            ShowSessionManagement();
        }

        private void ShowAttendance()
        {
            CurrentView = new AttendanceViewModel();
            CurrentViewName = "Attendance";
        }

        private void ShowSessionManagement()
        {
            try
            {
                CurrentView = new SessionManagementViewModel();
                CurrentViewName = "Class Sessions";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error loading Class Sessions:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                CurrentViewName = "Error";
            }
        }

        private void Logout()
        {
            var authService = new AuthenticationService();
            authService.Logout();
            
            // Close current window and open WelcomeWindow
            var currentWindow = System.Windows.Application.Current.MainWindow;
            var welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
            currentWindow?.Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
