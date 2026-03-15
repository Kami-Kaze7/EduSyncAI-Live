using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Collections.Generic;

namespace EduSyncAI
{
    public partial class RegistrationWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly GeminiFaceRecognitionService _faceService;
        private string? _selectedPhotoPath;
        private DispatcherTimer? _cameraPreviewTimer;
        private bool _isCameraActive = false;
        private string? _capturedImageData;
        
        // Multi-angle capture
        private int _currentPoseIndex = 0;
        private readonly string[] _poses = { "Front", "Left", "Right", "Up", "Down" };
        private readonly Dictionary<string, string> _capturedPoses = new();
        private bool _isCapturingSequence = false;

        public RegistrationWindow()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
            _faceService = new GeminiFaceRecognitionService();
            
            // Toggle fields based on role selection
            LecturerRadio.Checked += (s, e) => { LecturerFields.Visibility = Visibility.Visible; StudentFields.Visibility = Visibility.Collapsed; };
            StudentRadio.Checked += (s, e) => { LecturerFields.Visibility = Visibility.Collapsed; StudentFields.Visibility = Visibility.Visible; };
        }

        private async void StartRegistrationCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CaptureStatusText.Text = "Starting camera...";
                var started = await _faceService.StartCameraAsync();
                
                if (started)
                {
                    _isCameraActive = true;
                    CameraPlaceholderText.Visibility = Visibility.Collapsed;
                    
                    // Start preview timer
                    _cameraPreviewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                    _cameraPreviewTimer.Tick += async (s, args) => await UpdateCameraPreview();
                    _cameraPreviewTimer.Start();
                    
                    // Update UI
                    StartCameraButton.Visibility = Visibility.Collapsed;
                    StopCameraButton.Visibility = Visibility.Visible;
                    CaptureFaceButton.IsEnabled = true;
                    CaptureStatusText.Text = "✅ Camera active - Position your face in the frame and click 'Capture Face'";
                }
                else
                {
                    MessageBox.Show("Failed to start camera. Please check:\n\n• Camera is connected\n• Camera permissions are granted\n• No other app is using the camera",
                        "Camera Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    CaptureStatusText.Text = "❌ Camera failed to start";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting camera: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CaptureStatusText.Text = "❌ Camera error";
            }
        }

        private async System.Threading.Tasks.Task UpdateCameraPreview()
        {
            if (!_isCameraActive) return;
            
            try
            {
                var frameData = await _faceService.GetCurrentFrameAsync();
                if (!string.IsNullOrEmpty(frameData))
                {
                    var imageBytes = Convert.FromBase64String(frameData.Split(',')[1]);
                    using var ms = new MemoryStream(imageBytes);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    RegistrationCameraPreview.Source = bitmap;
                }
            }
            catch
            {
                // Silently continue - preview errors are non-critical
            }
        }

        private async void CaptureFace_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCameraActive)
            {
                MessageBox.Show("Please start the camera first.", "Camera Not Active", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_isCapturingSequence)
            {
                // Start multi-angle capture sequence
                _isCapturingSequence = true;
                _currentPoseIndex = 0;
                _capturedPoses.Clear();
                CaptureFaceButton.Content = "📸 Capture This Pose";
                
                ShowNextPoseInstruction();
            }
            else
            {
                // Capture current pose
                await CaptureCurrentPose();
            }
        }

        private void ShowNextPoseInstruction()
        {
            if (_currentPoseIndex >= _poses.Length)
            {
                // All poses captured
                CompleteFaceCapture();
                return;
            }

            var pose = _poses[_currentPoseIndex];
            var instruction = pose switch
            {
                "Front" => "👤 Look straight at the camera\n\nKeep your face centered and look directly forward",
                "Left" => "⬅️ Turn your face to the LEFT\n\nTurn your head about 45° to your left",
                "Right" => "➡️ Turn your face to the RIGHT\n\nTurn your head about 45° to your right",
                "Up" => "⬆️ Tilt your face UP\n\nLift your chin and look slightly upward",
                "Down" => "⬇️ Tilt your face DOWN\n\nLower your chin and look slightly downward",
                _ => "Look at the camera"
            };

            CaptureStatusText.Text = $"Pose {_currentPoseIndex + 1}/5: {instruction}";
            CaptureStatusText.FontSize = 13;
            CaptureStatusText.FontWeight = FontWeights.Bold;
        }

        private async System.Threading.Tasks.Task CaptureCurrentPose()
        {
            try
            {
                CaptureFaceButton.IsEnabled = false;
                var pose = _poses[_currentPoseIndex];
                
                CaptureStatusText.Text = $"📸 Capturing {pose} pose...";
                
                // Capture current frame
                var imageData = await _faceService.GetCurrentFrameAsync();
                
                if (string.IsNullOrEmpty(imageData))
                {
                    MessageBox.Show("Failed to capture image. Please try again.", "Capture Failed", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    CaptureStatusText.Text = "❌ Capture failed - Try again";
                    CaptureFaceButton.IsEnabled = true;
                    return;
                }

                // Store this pose
                _capturedPoses[pose] = imageData;
                
                // Show success and move to next pose
                CaptureStatusText.Text = $"✅ {pose} pose captured! ({_currentPoseIndex + 1}/5)";
                
                await System.Threading.Tasks.Task.Delay(1000); // Brief pause
                
                _currentPoseIndex++;
                CaptureFaceButton.IsEnabled = true;
                
                ShowNextPoseInstruction();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing pose: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CaptureFaceButton.IsEnabled = true;
            }
        }

        private void CompleteFaceCapture()
        {
            _isCapturingSequence = false;
            
            // Use front-facing image as primary
            _capturedImageData = _capturedPoses["Front"];
            
            // Show preview of front image
            try
            {
                var imageBytes = Convert.FromBase64String(_capturedImageData.Split(',')[1]);
                using var ms = new MemoryStream(imageBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                
                CapturedFacePreview.Source = bitmap;
                CapturedPreviewBorder.Visibility = Visibility.Visible;
            }
            catch { }
            
            CaptureStatusText.Text = "✅ All 5 poses captured successfully! You can now register.";
            CaptureStatusText.FontSize = 12;
            CaptureStatusText.FontWeight = FontWeights.Normal;
            CaptureFaceButton.Content = "📸 Capture Face";
            CaptureFaceButton.IsEnabled = true;
            
            MessageBox.Show("✅ Face registration complete!\n\n" +
                "Captured 5 different angles:\n" +
                "• Front view\n" +
                "• Left profile\n" +
                "• Right profile\n" +
                "• Looking up\n" +
                "• Looking down\n\n" +
                "This ensures accurate facial recognition during attendance.",
                "Multi-Angle Capture Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void StopRegistrationCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cameraPreviewTimer?.Stop();
                _cameraPreviewTimer = null;
                _isCameraActive = false;
                
                await _faceService.StopCameraAsync();
                
                RegistrationCameraPreview.Source = null;
                CameraPlaceholderText.Visibility = Visibility.Visible;
                StartCameraButton.Visibility = Visibility.Visible;
                StopCameraButton.Visibility = Visibility.Collapsed;
                CaptureFaceButton.IsEnabled = false;
                CaptureStatusText.Text = "Camera stopped. Click 'Start Camera' to capture again.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping camera: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChoosePhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Select Student Photo"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Check file size (max 5MB)
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Photo size must be less than 5MB", "File Too Large", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _selectedPhotoPath = openFileDialog.FileName;
                    // PhotoFileNameText.Text = Path.GetFileName(_selectedPhotoPath);
                    
                    // Show preview
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_selectedPhotoPath);
                    bitmap.DecodePixelWidth = 150;
                    bitmap.EndInit();
                    // PhotoPreviewImage.Source = bitmap;
                    // PhotoPreviewBorder.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading photo: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            
            // Validate common fields
            if (string.IsNullOrWhiteSpace(FullNameBox.Text) || 
                string.IsNullOrWhiteSpace(EmailBox.Text) || 
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ErrorText.Text = "Please fill in all required fields (Full Name, Email, Password)";
                return;
            }

            try
            {
                if (LecturerRadio.IsChecked == true)
                {
                    // Register Lecturer
                    if (string.IsNullOrWhiteSpace(UsernameBox.Text))
                    {
                        ErrorText.Text = "Username is required for lecturers";
                        return;
                    }

                    _authService.CreateLecturer(
                        UsernameBox.Text,
                        EmailBox.Text,
                        FullNameBox.Text,
                        PasswordBox.Password,
                        string.IsNullOrWhiteSpace(PinBox.Text) ? null : PinBox.Text
                    );

                    MessageBox.Show($"Lecturer account created successfully!\n\nUsername: {UsernameBox.Text}\n\nYou can now login.", 
                        "Registration Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Register Student
                    if (string.IsNullOrWhiteSpace(MatricBox.Text))
                    {
                        ErrorText.Text = "Matric Number is required for students";
                        return;
                    }

                    // MANDATORY: Face capture required for facial recognition
                    if (string.IsNullOrEmpty(_capturedImageData) || _capturedPoses.Count < 5)
                    {
                        MessageBox.Show("📷 Multi-Angle Face Capture is REQUIRED!\n\n" +
                            "Please:\n" +
                            "1. Click 'Start Camera'\n" +
                            "2. Click 'Capture Face' to begin\n" +
                            "3. Follow the on-screen instructions\n" +
                            "4. Capture all 5 poses (Front, Left, Right, Up, Down)\n\n" +
                            "This ensures accurate attendance recognition.",
                            "Face Capture Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Save all captured poses
                    var photosDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "StudentPhotos");
                    Directory.CreateDirectory(photosDir);
                    
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var photoPath = "";
                    
                    // Sanitize matric number for filename (replace / with _)
                    var sanitizedMatric = MatricBox.Text.Replace("/", "_").Replace("\\", "_");
                    
                    // Save each pose
                    foreach (var pose in _capturedPoses)
                    {
                        var fileName = $"{sanitizedMatric}_{pose.Key}_{timestamp}.jpg";
                        var filePath = Path.Combine(photosDir, fileName);
                        
                        // Convert base64 to image file
                        var imageBytes = Convert.FromBase64String(pose.Value.Split(',')[1]);
                        File.WriteAllBytes(filePath, imageBytes);
                        
                        // Use front image as primary photo path
                        if (pose.Key == "Front")
                        {
                            photoPath = filePath;
                        }
                    }

                    int studentId = _authService.RegisterStudent(
                        MatricBox.Text,
                        FullNameBox.Text,
                        EmailBox.Text,
                        PasswordBox.Password,
                        string.IsNullOrWhiteSpace(PinBox.Text) ? null : PinBox.Text
                    );

                    // Update photo path if saved
                    if (photoPath != null)
                    {
                        var dbService = new DatabaseService();
                        dbService.UpdateStudentPhoto(studentId, photoPath);
                    }

                    MessageBox.Show($"Student account created successfully!\n\nMatric Number: {MatricBox.Text}\n\nYou can now login.",
                        "Registration Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Go to login
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Registration failed: {ex.Message}";
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
            this.Close();
        }
    }
}
