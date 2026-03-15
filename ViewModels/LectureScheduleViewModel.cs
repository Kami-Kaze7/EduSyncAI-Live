using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace EduSyncAI
{
    public class LectureScheduleViewModel
    {
    private readonly DatabaseService _dbService;

    public ObservableCollection<Course> Courses { get; set; }
    public ObservableCollection<Lecture> Lectures { get; set; }
    public Course SelectedCourse { get; set; }
    
    public string NewLectureTopic { get; set; }
    public DateTime NewLectureDate { get; set; }

    public ICommand AddLectureCommand { get; }
    public ICommand RefreshLecturesCommand { get; }

    public LectureScheduleViewModel()
    {
        _dbService = new DatabaseService();
        Courses = new ObservableCollection<Course>();
        Lectures = new ObservableCollection<Lecture>();
        NewLectureDate = DateTime.Now.AddDays(1); // Default to tomorrow

        AddLectureCommand = new RelayCommand(AddLecture);
        RefreshLecturesCommand = new RelayCommand(LoadLectures);

        LoadCourses();
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

            if (Courses.Any())
            {
                SelectedCourse = Courses.First();
                LoadLectures();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading courses: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void LoadLectures()
    {
        if (SelectedCourse == null) return;

        try
        {
            var lectures = _dbService.GetLecturesByCourse(SelectedCourse.Id);
            Lectures.Clear();
            foreach (var lecture in lectures)
            {
                Lectures.Add(lecture);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading lectures: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void AddLecture()
    {
        if (SelectedCourse == null)
        {
            System.Windows.MessageBox.Show("Please select a course first.", "Validation Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(NewLectureTopic))
        {
            System.Windows.MessageBox.Show("Please enter a lecture topic.", "Validation Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var lecture = new Lecture
        {
            CourseId = SelectedCourse.Id,
            LectureDate = NewLectureDate,
            Topic = NewLectureTopic
        };

        try
        {
            var lectureId = _dbService.CreateLecture(lecture);
            lecture.Id = lectureId;
            Lectures.Add(lecture);

            System.Windows.MessageBox.Show($"Lecture '{NewLectureTopic}' added successfully!", "Success",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            // Clear form
            NewLectureTopic = string.Empty;
            NewLectureDate = DateTime.Now.AddDays(1);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error adding lecture: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
}
