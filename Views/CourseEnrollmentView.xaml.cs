using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EduSyncAI
{
    public partial class CourseEnrollmentView : UserControl
    {
        private readonly DatabaseService _dbService;
        private readonly AuthenticationService _authService;
        private Student? _currentStudent;

        public class CourseDisplayItem
        {
            public int CourseId { get; set; }
            public string CourseTitle { get; set; }
            public string CourseCode { get; set; }
            public bool IsEnrolled { get; set; }
            public string ButtonText => IsEnrolled ? "✓ Enrolled" : "✓ Enroll";
            public string ButtonColor => IsEnrolled ? "#E74C3C" : "#27AE60";
            public bool CanEnroll => !IsEnrolled;
        }

        public CourseEnrollmentView()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _authService = new AuthenticationService();
            _currentStudent = _authService.GetCurrentStudent();

            LoadCourses();
        }

        private void LoadCourses()
        {
            try
            {
                var courses = _dbService.GetAllCourses();
                var displayItems = new List<CourseDisplayItem>();

                foreach (var course in courses)
                {
                    bool isEnrolled = false;
                    if (_currentStudent != null)
                    {
                        isEnrolled = _dbService.IsStudentEnrolled(_currentStudent.Id, course.Id);
                    }

                    displayItems.Add(new CourseDisplayItem
                    {
                        CourseId = course.Id,
                        CourseTitle = course.CourseTitle,
                        CourseCode = course.CourseCode,
                        IsEnrolled = isEnrolled
                    });
                }

                CoursesItemsControl.ItemsSource = displayItems;
                StatusText.Text = $"{courses.Count} course(s) available";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading courses: {ex.Message}";
            }
        }

        private void EnrollButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStudent == null)
            {
                MessageBox.Show("Please login first", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag is int courseId)
            {
                try
                {
                    _dbService.EnrollStudent(_currentStudent.Id, courseId);
                    MessageBox.Show("Successfully enrolled in course!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCourses(); // Refresh to show updated status
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Already Enrolled", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Enrollment failed: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
