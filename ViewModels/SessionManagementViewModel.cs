using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace EduSyncAI
{
    public class SessionManagementViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly SessionManagementService _sessionService;
        private DispatcherTimer _liveTimer;

        private ObservableCollection<Course> _courses;
        private Course _selectedCourse;
        private ClassSession _activeSession;
        private string _sessionDuration;
        private ObservableCollection<ClassSession> _sessionHistory;

        public ObservableCollection<Course> Courses
        {
            get => _courses;
            set { _courses = value; OnPropertyChanged(nameof(Courses)); }
        }


        public Course SelectedCourse
        {
            get => _selectedCourse;
            set
            {
                _selectedCourse = value;
                OnPropertyChanged(nameof(SelectedCourse));
            }
        }


        public ClassSession ActiveSession
        {
            get => _activeSession;
            set
            {
                _activeSession = value;
                OnPropertyChanged(nameof(ActiveSession));
                OnPropertyChanged(nameof(HasActiveSession));
                OnPropertyChanged(nameof(CanCreateSession));
            }
        }

        public string SessionDuration
        {
            get => _sessionDuration;
            set { _sessionDuration = value; OnPropertyChanged(nameof(SessionDuration)); }
        }

        public ObservableCollection<ClassSession> SessionHistory
        {
            get => _sessionHistory;
            set { _sessionHistory = value; OnPropertyChanged(nameof(SessionHistory)); }
        }

        public bool HasActiveSession => ActiveSession != null;
        public bool CanCreateSession => !HasActiveSession;

        public ICommand StartSessionCommand { get; }
        public ICommand EndSessionCommand { get; }
        public ICommand RefreshCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SessionManagementViewModel()
        {
            _dbService = new DatabaseService();
            _sessionService = new SessionManagementService();

            Courses = new ObservableCollection<Course>();
            SessionHistory = new ObservableCollection<ClassSession>();

            StartSessionCommand = new RelayCommand(StartSession);
            EndSessionCommand = new RelayCommand(EndSession);
            RefreshCommand = new RelayCommand(LoadData);

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Load courses
                var courses = _dbService.GetAllCourses();
                Courses.Clear();
                foreach (var course in courses)
                {
                    Courses.Add(course);
                }

                // Load active session
                ActiveSession = _sessionService.GetActiveSession();
                if (ActiveSession != null)
                {
                    StartLiveTimer();
                }

                // Load session history
                var sessions = _sessionService.GetAllSessions();
                SessionHistory.Clear();
                foreach (var session in sessions)
                {
                    SessionHistory.Add(session);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }


        private void StartSession()
        {
            if (SelectedCourse == null)
            {
                System.Windows.MessageBox.Show("Please select a course first.", "Select Course",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Check if there's an existing 'Ready' session for this course
                var readySession = _sessionService.GetAllSessions()
                    .FirstOrDefault(s => s.CourseId == SelectedCourse.Id && s.State == SessionState.Ready);

                int sessionId;

                if (readySession != null)
                {
                    sessionId = readySession.Id;
                }
                else
                {
                    // 2. No ready session found, let's create one automatically
                    var lectures = _dbService.GetLecturesByCourse(SelectedCourse.Id);
                    int lectureId;

                    if (lectures.Any())
                    {
                        // Use the most recent/relevant lecture or just the first one
                        lectureId = lectures.First().Id;
                    }
                    else
                    {
                        // Create a default lecture if none exists
                        var defaultLecture = new Lecture
                        {
                            CourseId = SelectedCourse.Id,
                            LectureDate = DateTime.Now,
                            Topic = "Introductory Session"
                        };
                        lectureId = _dbService.CreateLecture(defaultLecture);
                    }

                    var authService = new AuthenticationService();
                    var currentLecturer = authService.GetCurrentLecturer();
                    sessionId = _sessionService.CreateSession(SelectedCourse.Id, lectureId, "", currentLecturer?.Id);
                }

                // 3. Start the session
                _sessionService.StartSession(sessionId);
                
                var session = _sessionService.GetSessionById(sessionId);
                System.Windows.MessageBox.Show($"Session started successfully!\n\nCourse: {session.CourseName}\nLecture: {session.LectureTopic}", 
                    "Session Live", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                LoadData();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error starting session: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void EndSession()
        {
            if (ActiveSession == null)
            {
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"End this session?\n\nCourse: {ActiveSession.CourseName}\nLecture: {ActiveSession.LectureTopic}",
                "Confirm End Session",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _sessionService.EndSession(ActiveSession.Id);
                    StopLiveTimer();

                    var endedSession = _sessionService.GetSessionById(ActiveSession.Id);
                    var duration = _sessionService.GetFormattedDuration(endedSession.Duration);

                    System.Windows.MessageBox.Show($"Session ended!\n\nDuration: {duration}", 
                        "Session Ended", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    LoadData();

                    // Upload attendance records to cloud
                    _ = UploadAttendanceRecordsAsync(endedSession.Id);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error ending session: {ex.Message}", "Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void StartLiveTimer()
        {
            if (_liveTimer != null)
            {
                _liveTimer.Stop();
            }

            _liveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _liveTimer.Tick += UpdateSessionDuration;
            _liveTimer.Start();
        }

        private void StopLiveTimer()
        {
            if (_liveTimer != null)
            {
                _liveTimer.Stop();
                _liveTimer = null;
            }
            SessionDuration = "";
        }

        private void UpdateSessionDuration(object sender, EventArgs e)
        {
            if (ActiveSession?.StartTime != null)
            {
                var elapsed = DateTime.Now - ActiveSession.StartTime.Value;
                SessionDuration = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            }
        }

        private async Task UploadAttendanceRecordsAsync(int sessionId)
        {
            try
            {
                var session = _sessionService.GetSessionById(sessionId);
                var records = _dbService.GetSessionAttendance(sessionId);
                if (records == null || records.Count == 0) return;

                // Create a DTO that matches the Web API's AttendanceUploadDto
                var uploadData = new
                {
                    SessionInfo = session,
                    Records = records
                };

                using (var client = new HttpClient())
                {
                    // Update this to your local API URL
                    client.BaseAddress = new Uri("http://localhost:5152/");
                    var json = JsonSerializer.Serialize(uploadData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"api/attendance/session/{sessionId}", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[Cloud] Failed to sync attendance: {error}");
                    }
                    else
                    {
                        Console.WriteLine($"[Cloud] Successfully synced attendance for session {sessionId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cloud] Error syncing attendance: {ex.Message}");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
