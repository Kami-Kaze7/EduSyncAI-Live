using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace EduSyncAI
{
    public class LecturerPrepViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly EmailService _emailService;
        private readonly TimerService _timerService;

        private string? _coreIdeas;
        private string? _keyTerms;
        private string? _simpleExample;
        private string? _whatToListenFor;
        private string? _timeRemaining;
        private Lecture? _selectedLecture;

        public string? CoreIdeas
        {
            get => _coreIdeas;
            set { _coreIdeas = value; OnPropertyChanged(nameof(CoreIdeas)); }
        }

        public string? KeyTerms
        {
            get => _keyTerms;
            set { _keyTerms = value; OnPropertyChanged(nameof(KeyTerms)); }
        }

        public string? SimpleExample
        {
            get => _simpleExample;
            set { _simpleExample = value; OnPropertyChanged(nameof(SimpleExample)); }
        }

        public string? WhatToListenFor
        {
            get => _whatToListenFor;
            set { _whatToListenFor = value; OnPropertyChanged(nameof(WhatToListenFor)); }
        }

        public string? TimeRemaining
        {
            get => _timeRemaining;
            set { _timeRemaining = value; OnPropertyChanged(nameof(TimeRemaining)); }
        }

        public Lecture? SelectedLecture
        {
            get => _selectedLecture;
            set { _selectedLecture = value; OnPropertyChanged(nameof(SelectedLecture)); }
        }

        public ObservableCollection<Lecture> AvailableLectures { get; set; }
        public ICommand SaveCommand { get; }
        public ICommand StartTimerCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public LecturerPrepViewModel()
        {
            _dbService = new DatabaseService();
            _emailService = new EmailService();
            _timerService = new TimerService();
            AvailableLectures = new ObservableCollection<Lecture>();

            SaveCommand = new RelayCommand(Save);
            StartTimerCommand = new RelayCommand(StartTimer);

            _timerService.TimeChanged += OnTimeChanged;
            _timerService.WarningTriggered += OnWarningTriggered;
            _timerService.TimerExpired += OnTimerExpired;

            TimeRemaining = "05:00";
            LoadLectures();
        }

        private void LoadLectures()
        {
            try
            {
                var courses = _dbService.GetAllCourses();
                AvailableLectures.Clear();
                
                foreach (var course in courses)
                {
                    var lectures = _dbService.GetLecturesByCourse(course.Id);
                    foreach (var lecture in lectures)
                    {
                        AvailableLectures.Add(lecture);
                    }
                }

                if (AvailableLectures.Any())
                {
                    SelectedLecture = AvailableLectures.First();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading lectures: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void StartTimer()
        {
            if (!_timerService.IsRunning)
            {
                _timerService.Start();
                System.Windows.MessageBox.Show("5-minute timer started! Work efficiently.", "Timer Started",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void OnTimeChanged(object sender, int secondsRemaining)
        {
            TimeRemaining = _timerService.GetFormattedTime();
        }

        private void OnWarningTriggered(object sender, string warning)
        {
            System.Windows.MessageBox.Show(warning, "Time Warning",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        private void OnTimerExpired(object sender, EventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Time's up! Would you like to save your prep now?",
                "Timer Expired",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Save();
            }
        }

        private void Save()
        {
            if (SelectedLecture == null)
            {
                System.Windows.MessageBox.Show("Please select a lecture first.", "Validation Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CoreIdeas) || string.IsNullOrWhiteSpace(KeyTerms))
            {
                System.Windows.MessageBox.Show("Please fill in at least Core Ideas and Key Terms.", "Validation Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                var prep = new LecturePrep
                {
                    LectureId = SelectedLecture.Id,
                    CoreIdeas = CoreIdeas,
                    KeyTerms = KeyTerms,
                    SimpleExample = SimpleExample ?? "",
                    WhatToListenFor = WhatToListenFor ?? "",
                    CreatedAt = DateTime.Now
                };

                _dbService.SaveLecturePrep(prep);

                var lecture = AvailableLectures.FirstOrDefault(l => l.Id == SelectedLecture.Id);
                if (lecture != null)
                {
                    var students = _dbService.GetStudentsByCourse(lecture.CourseId);

                    if (students.Any())
                    {
                        _emailService.SendLecturePrepNotification(students, prep, lecture.Topic, lecture.LectureDate);
                        
                        System.Windows.MessageBox.Show(
                            $"Lecture prep saved and {students.Count} student(s) notified!",
                            "Success",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "Lecture prep saved! (No students enrolled in this course yet)",
                            "Success",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                }

                _timerService.Reset();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving lecture prep: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            CoreIdeas = string.Empty;
            KeyTerms = string.Empty;
            SimpleExample = string.Empty;
            WhatToListenFor = string.Empty;
            TimeRemaining = "05:00";
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
