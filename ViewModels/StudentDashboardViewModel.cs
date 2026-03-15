using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace EduSyncAI
{
    public class StudentDashboardViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly StudentImportService _importService;

        private ObservableCollection<Course> _courses;
        private Course? _selectedCourse;
        private ObservableCollection<Student> _students;
        private Student? _selectedStudent;
        
        // Manual entry fields
        private string _newMatricNumber;
        private string _newFullName;
        private string _newEmail;
        private string _newWindowsUsername;
        private string _statusMessage;

        public ObservableCollection<Course> Courses
        {
            get => _courses;
            set { _courses = value; OnPropertyChanged(nameof(Courses)); }
        }

        public Course? SelectedCourse
        {
            get => _selectedCourse;
            set { _selectedCourse = value; OnPropertyChanged(nameof(SelectedCourse)); LoadStudents(); }
        }

        public ObservableCollection<Student> Students
        {
            get => _students;
            set { _students = value; OnPropertyChanged(nameof(Students)); }
        }

        public Student? SelectedStudent
        {
            get => _selectedStudent;
            set { _selectedStudent = value; OnPropertyChanged(nameof(SelectedStudent)); }
        }

        public string NewMatricNumber
        {
            get => _newMatricNumber;
            set { _newMatricNumber = value; OnPropertyChanged(nameof(NewMatricNumber)); }
        }

        public string NewFullName
        {
            get => _newFullName;
            set { _newFullName = value; OnPropertyChanged(nameof(NewFullName)); }
        }

        public string NewEmail
        {
            get => _newEmail;
            set { _newEmail = value; OnPropertyChanged(nameof(NewEmail)); }
        }

        public string NewWindowsUsername
        {
            get => _newWindowsUsername;
            set { _newWindowsUsername = value; OnPropertyChanged(nameof(NewWindowsUsername)); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public ICommand UploadExcelCommand { get; }
        public ICommand AddStudentCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand RefreshCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public StudentDashboardViewModel()
        {
            _dbService = new DatabaseService();
            _importService = new StudentImportService();
            _courses = new ObservableCollection<Course>();
            _students = new ObservableCollection<Student>();
            _newMatricNumber = "";
            _newFullName = "";
            _newEmail = "";
            _newWindowsUsername = "";
            _statusMessage = "";

            UploadExcelCommand = new RelayCommand(UploadExcel);
            AddStudentCommand = new RelayCommand(AddStudent);
            DeleteStudentCommand = new RelayCommand(DeleteStudent);
            RefreshCommand = new RelayCommand(LoadStudents);

            LoadCourses();
        }

        private void LoadCourses()
        {
            var courses = _dbService.GetAllCourses();
            Courses.Clear();
            foreach (var course in courses)
            {
                Courses.Add(course);
            }

            if (Courses.Any())
            {
                SelectedCourse = Courses[0];
            }
        }

        private void LoadStudents()
        {
            if (SelectedCourse == null) return;

            var students = _dbService.GetEnrolledStudents(SelectedCourse.Id);
            Students.Clear();
            foreach (var student in students)
            {
                Students.Add(student);
            }

            StatusMessage = $"{students.Count} student(s) enrolled in {SelectedCourse.CourseTitle}";
        }

        private void UploadExcel()
        {
            if (SelectedCourse == null)
            {
                MessageBox.Show("Please select a course first", "No Course Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Student List File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    StatusMessage = "Importing students...";
                    
                    StudentImportService.ImportResult result;
                    
                    if (openFileDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        result = _importService.ImportFromCSV(openFileDialog.FileName, SelectedCourse.Id);
                    }
                    else
                    {
                        result = _importService.ImportFromExcel(openFileDialog.FileName, SelectedCourse.Id);
                    }

                    // Show results
                    string message = $"Import Complete!\n\n" +
                                   $"✓ Successfully imported: {result.SuccessCount} students\n" +
                                   $"✗ Errors: {result.ErrorCount}\n";

                    if (result.Errors.Any())
                    {
                        message += "\nErrors:\n" + string.Join("\n", result.Errors.Take(10));
                        if (result.Errors.Count > 10)
                        {
                            message += $"\n... and {result.Errors.Count - 10} more errors";
                        }
                    }

                    MessageBox.Show(message, "Import Results", 
                        MessageBoxButton.OK, 
                        result.ErrorCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

                    LoadStudents();
                }
                catch (Exception ex)
                {
                    StatusMessage = "Import failed";
                    MessageBox.Show($"Error importing file:\n\n{ex.Message}", "Import Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddStudent()
        {
            if (SelectedCourse == null)
            {
                MessageBox.Show("Please select a course first", "No Course Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewMatricNumber) || 
                string.IsNullOrWhiteSpace(NewFullName) || 
                string.IsNullOrWhiteSpace(NewEmail))
            {
                MessageBox.Show("Please fill in all required fields (Matric Number, Full Name, Email)", 
                    "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Check if student already exists
                var existing = _dbService.GetAllStudents();
                if (existing.Any(s => s.MatricNumber == NewMatricNumber))
                {
                    MessageBox.Show($"Student with matric number {NewMatricNumber} already exists", 
                        "Duplicate Student", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var student = new Student
                {
                    MatricNumber = NewMatricNumber,
                    FullName = NewFullName,
                    Email = NewEmail,
                    WindowsUsername = string.IsNullOrWhiteSpace(NewWindowsUsername) ? null : NewWindowsUsername
                };

                int studentId = _dbService.CreateStudent(student);
                _dbService.EnrollStudent(studentId, SelectedCourse.Id);

                StatusMessage = $"✓ Added {student.FullName} to {SelectedCourse.CourseTitle}";
                
                // Clear form
                NewMatricNumber = "";
                NewFullName = "";
                NewEmail = "";
                NewWindowsUsername = "";

                LoadStudents();
                
                MessageBox.Show($"Student {student.FullName} added successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to add student";
                MessageBox.Show($"Error adding student:\n\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteStudent()
        {
            if (SelectedStudent == null)
            {
                MessageBox.Show("Please select a student to delete", "No Student Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete {SelectedStudent.FullName}?\n\nThis will remove the student from the system entirely.", 
                "Confirm Delete", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Note: You'll need to add DeleteStudent method to DatabaseService
                    // For now, just show a message
                    MessageBox.Show("Delete functionality will be implemented in DatabaseService", 
                        "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // TODO: _dbService.DeleteStudent(SelectedStudent.Id);
                    // LoadStudents();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting student:\n\n{ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
