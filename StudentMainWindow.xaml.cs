using System;
using System.Windows;
using System.Windows.Controls;

namespace EduSyncAI
{
    public partial class StudentMainWindow : Window
    {
        private readonly AuthenticationService _authService;
        private Student? _currentStudent;

        public StudentMainWindow()
        {
            try
            {
                InitializeComponent();
                _authService = new AuthenticationService();
                _currentStudent = _authService.GetCurrentStudent();

                if (_currentStudent != null)
                {
                    StudentNameText.Text = $"Welcome, {_currentStudent.FullName}";
                }

                // Show browse courses by default
                ShowBrowseCourses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Student Dashboard:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void ShowBrowseCourses_Click(object sender, RoutedEventArgs e)
        {
            ShowBrowseCourses();
        }

        private void ShowBrowseCourses()
        {
            try
            {
                var view = new CourseEnrollmentView();
                MainContent.Content = view;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading courses:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMyCourses_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var view = new MyEnrolledCoursesView();
                MainContent.Content = view;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading enrolled courses:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMyAttendance_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("My Attendance view - Coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            _authService.LogoutStudent();
            var welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
            this.Close();
        }
    }
}
