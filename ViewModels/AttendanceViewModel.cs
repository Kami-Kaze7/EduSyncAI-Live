using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace EduSyncAI
{
    public class AttendanceViewModel : INotifyPropertyChanged
    {
        private readonly AttendanceService _attendanceService;
        private readonly DatabaseService _dbService;
        private DispatcherTimer _timer;

        private ClassSession? _activeSession;
        private string _sessionInfo;
        private string _attendanceStatus;
        private int _presentCount;
        private int _enrolledCount;
        private ObservableCollection<AttendanceRecord> _attendanceList;
        private ObservableCollection<Student> _enrolledStudents;
        private Student? _selectedStudent;
        private string _searchText;
        private bool _isFingerprintReady;
        private string _fingerprintMessage;

        public ClassSession? ActiveSession
        {
            get => _activeSession;
            set { _activeSession = value; OnPropertyChanged(nameof(ActiveSession)); UpdateSessionInfo(); }
        }

        public string SessionInfo
        {
            get => _sessionInfo;
            set { _sessionInfo = value; OnPropertyChanged(nameof(SessionInfo)); }
        }

        public string AttendanceStatus
        {
            get => _attendanceStatus;
            set { _attendanceStatus = value; OnPropertyChanged(nameof(AttendanceStatus)); }
        }

        public int PresentCount
        {
            get => _presentCount;
            set { _presentCount = value; OnPropertyChanged(nameof(PresentCount)); }
        }

        public int EnrolledCount
        {
            get => _enrolledCount;
            set { _enrolledCount = value; OnPropertyChanged(nameof(EnrolledCount)); }
        }

        public ObservableCollection<AttendanceRecord> AttendanceList
        {
            get => _attendanceList;
            set { _attendanceList = value; OnPropertyChanged(nameof(AttendanceList)); }
        }

        public ObservableCollection<Student> EnrolledStudents
        {
            get => _enrolledStudents;
            set { _enrolledStudents = value; OnPropertyChanged(nameof(EnrolledStudents)); }
        }

        public Student? SelectedStudent
        {
            get => _selectedStudent;
            set { _selectedStudent = value; OnPropertyChanged(nameof(SelectedStudent)); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); FilterStudents(); }
        }

        public bool IsFingerprintReady
        {
            get => _isFingerprintReady;
            set { _isFingerprintReady = value; OnPropertyChanged(nameof(IsFingerprintReady)); }
        }

        public string FingerprintMessage
        {
            get => _fingerprintMessage;
            set { _fingerprintMessage = value; OnPropertyChanged(nameof(FingerprintMessage)); }
        }

        public ICommand CheckInWithFingerprintCommand { get; }
        public ICommand MarkManuallyCommand { get; }
        public ICommand RefreshCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AttendanceViewModel()
        {
            _attendanceService = new AttendanceService();
            _dbService = new DatabaseService();
            _attendanceList = new ObservableCollection<AttendanceRecord>();
            _enrolledStudents = new ObservableCollection<Student>();
            _sessionInfo = "";
            _attendanceStatus = "";
            _searchText = "";
            _fingerprintMessage = "Ready to scan fingerprint";

            CheckInWithFingerprintCommand = new RelayCommand(async () => await CheckInWithFingerprintAsync());
            MarkManuallyCommand = new RelayCommand(MarkManually);
            RefreshCommand = new RelayCommand(LoadAttendance);

            LoadActiveSession();
            CheckBiometricAvailability();
        }

        private async void CheckBiometricAvailability()
        {
            var biometricService = new BiometricAuthenticationService();
            IsFingerprintReady = await biometricService.IsBiometricAvailableAsync();
            
            if (!IsFingerprintReady)
            {
                FingerprintMessage = "Fingerprint scanner not available";
            }
        }


        private void LoadActiveSession()
        {
            var sessionService = new SessionManagementService();
            var session = sessionService.GetActiveSession();
            if (session == null || session.State != SessionState.Live)
            {
                AttendanceStatus = "No active session. Please start a session first.";
                return;
            }

            ActiveSession = session;
            LoadEnrolledStudents();
            LoadAttendance();
        }

        private void UpdateSessionInfo()
        {
            if (ActiveSession != null)
            {
                SessionInfo = $"{ActiveSession.CourseName} - {ActiveSession.LectureTopic}";
            }
        }


        private void LoadEnrolledStudents()
        {
            if (ActiveSession == null) return;

            var students = _attendanceService.GetEnrolledStudents(ActiveSession.CourseId);
            EnrolledCount = students.Count;
            EnrolledStudents.Clear();
            foreach (var student in students)
            {
                EnrolledStudents.Add(student);
            }
        }

        private void LoadAttendance()
        {
            if (ActiveSession == null) return;

            var records = _attendanceService.GetSessionAttendance(ActiveSession.Id);
            PresentCount = records.Count;
            
            AttendanceList.Clear();
            foreach (var record in records)
            {
                AttendanceList.Add(record);
            }

            AttendanceStatus = $"{PresentCount} / {EnrolledCount} students present";
        }

        private async Task CheckInWithFingerprintAsync()
        {
            if (ActiveSession == null)
            {
                MessageBox.Show("No active session", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsFingerprintReady)
            {
                MessageBox.Show("Fingerprint scanner not available", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FingerprintMessage = "Place your finger on the scanner...";

            try
            {
                var biometricService = new BiometricAuthenticationService();
                bool verified = await biometricService.AuthenticateWithBiometricAsync("Check in to class");

                if (verified)
                {
                    // Fingerprint verified! Now identify the student
                    // Get current Windows username
                    string windowsUser = Environment.UserName;
                    
                    // Find student by Windows username
                    var enrolledStudents = _attendanceService.GetEnrolledStudents(ActiveSession.CourseId);
                    var student = enrolledStudents.FirstOrDefault(s => 
                        s.WindowsUsername != null && 
                        s.WindowsUsername.Equals(windowsUser, StringComparison.OrdinalIgnoreCase));

                    if (student != null)
                    {
                        // Student found! Mark them present automatically
                        try
                        {
                            _attendanceService.MarkStudentPresent(ActiveSession.Id, student.Id, CheckInMethod.Fingerprint);
                            LoadAttendance();
                            FingerprintMessage = $"✓ {student.FullName} marked present!";
                            MessageBox.Show($"Welcome, {student.FullName}!\n\nYou have been marked present.", 
                                "Check-in Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (InvalidOperationException ex)
                        {
                            FingerprintMessage = ex.Message;
                            MessageBox.Show(ex.Message, "Already Present", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        // Student not found - they need to register their fingerprint first
                        FingerprintMessage = "Fingerprint not registered for this course. Please see your lecturer.";
                        MessageBox.Show("Your fingerprint is not registered for this course.\n\nPlease register during course enrollment.", 
                            "Not Registered", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    FingerprintMessage = "Fingerprint verification failed. Try again.";
                    MessageBox.Show("Fingerprint verification failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                FingerprintMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkManually()
        {
            if (ActiveSession == null)
            {
                MessageBox.Show("No active session", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedStudent == null)
            {
                MessageBox.Show("Please select a student", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _attendanceService.MarkStudentPresent(ActiveSession.Id, SelectedStudent.Id, CheckInMethod.Fingerprint);
                LoadAttendance();
                SelectedStudent = null;
                SearchText = "";
                FingerprintMessage = "Student marked present! Ready for next check-in.";
                MessageBox.Show($"{SelectedStudent?.FullName} marked present!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterStudents()
        {
            if (ActiveSession == null) return;

            var allStudents = _attendanceService.GetEnrolledStudents(ActiveSession.CourseId);
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                EnrolledStudents.Clear();
                foreach (var student in allStudents)
                {
                    EnrolledStudents.Add(student);
                }
            }
            else
            {
                var filtered = allStudents.Where(s => 
                    s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.MatricNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                EnrolledStudents.Clear();
                foreach (var student in filtered)
                {
                    EnrolledStudents.Add(student);
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
