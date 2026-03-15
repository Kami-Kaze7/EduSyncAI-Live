using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EduSyncAI
{
    public partial class MyEnrolledCoursesView : UserControl
    {
        private readonly DatabaseService _dbService;
        private readonly AuthenticationService _authService;
        private Student? _currentStudent;

        public class EnrolledCourseItem
        {
            public int CourseId { get; set; }
            public string CourseTitle { get; set; }
            public string CourseCode { get; set; }
        }

        public MyEnrolledCoursesView()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _authService = new AuthenticationService();
            _currentStudent = _authService.GetCurrentStudent();

            LoadEnrolledCourses();
        }

        private void LoadEnrolledCourses()
        {
            try
            {
                if (_currentStudent == null)
                {
                    StatusText.Text = "Please login to view your courses";
                    return;
                }

                var enrolledCourses = _dbService.GetEnrolledCourses(_currentStudent.Id);
                
                if (enrolledCourses.Count == 0)
                {
                    EnrolledCoursesItemsControl.Visibility = Visibility.Collapsed;
                    EmptyState.Visibility = Visibility.Visible;
                    StatusText.Text = "";
                }
                else
                {
                    var displayItems = enrolledCourses.Select(c => new EnrolledCourseItem
                    {
                        CourseId = c.Id,
                        CourseTitle = c.CourseTitle,
                        CourseCode = c.CourseCode
                    }).ToList();

                    EnrolledCoursesItemsControl.ItemsSource = displayItems;
                    EnrolledCoursesItemsControl.Visibility = Visibility.Visible;
                    EmptyState.Visibility = Visibility.Collapsed;
                    StatusText.Text = $"You are enrolled in {enrolledCourses.Count} course(s)";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading courses: {ex.Message}";
            }
        }

        private void Unenroll_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStudent == null)
            {
                MessageBox.Show("Please login first", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag is int courseId)
            {
                var result = MessageBox.Show("Are you sure you want to unenroll from this course?", 
                    "Confirm Unenroll", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dbService.UnenrollStudent(_currentStudent.Id, courseId);
                        MessageBox.Show("Successfully unenrolled from course", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadEnrolledCourses(); // Refresh list
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Unenroll failed: {ex.Message}", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
