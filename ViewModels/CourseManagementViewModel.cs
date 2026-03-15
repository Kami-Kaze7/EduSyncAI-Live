using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;

namespace EduSyncAI
{
    public class CourseManagementViewModel
    {
    private readonly DatabaseService _dbService;
    private readonly FileService _fileService;

    public string CourseCode { get; set; }
    public string CourseTitle { get; set; }
    public string SyllabusPath { get; set; }
    public ObservableCollection<Course> Courses { get; set; }

    public ICommand CreateCourseCommand { get; }
    public ICommand UploadSyllabusCommand { get; }

    public CourseManagementViewModel()
    {
        _dbService = new DatabaseService();
        _fileService = new FileService();
        Courses = new ObservableCollection<Course>();

        CreateCourseCommand = new RelayCommand(CreateCourse);
        UploadSyllabusCommand = new RelayCommand(UploadSyllabus);

        LoadCourses();
    }

    private void CreateCourse()
    {
        if (string.IsNullOrWhiteSpace(CourseCode) || string.IsNullOrWhiteSpace(CourseTitle))
        {
            System.Windows.MessageBox.Show("Please enter both course code and title.", "Validation Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var course = new Course
        {
            CourseCode = CourseCode,
            CourseTitle = CourseTitle,
            SyllabusPath = SyllabusPath
        };

        try
        {
            var courseId = _dbService.CreateCourse(course);
            course.Id = courseId;
            Courses.Add(course);

            System.Windows.MessageBox.Show($"Course '{CourseCode}' created successfully!", "Success",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            // Clear form
            CourseCode = string.Empty;
            CourseTitle = string.Empty;
            SyllabusPath = string.Empty;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error creating course: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void UploadSyllabus()
    {
        if (string.IsNullOrWhiteSpace(CourseCode))
        {
            System.Windows.MessageBox.Show("Please enter a course code first.", "Validation Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var openFileDialog = new OpenFileDialog
        {
            Filter = "Document Files|*.pdf;*.doc;*.docx",
            Title = "Select Syllabus File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                SyllabusPath = _fileService.SaveSyllabus(openFileDialog.FileName, CourseCode);
                System.Windows.MessageBox.Show("Syllabus uploaded successfully!", "Success",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error uploading syllabus: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void LoadCourses()
    {
        try
        {
            var courses = _dbService.GetAllCourses();
            Courses.Clear();
            foreach (var course in courses)
            {
                Courses.Add(course);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading courses: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
}
