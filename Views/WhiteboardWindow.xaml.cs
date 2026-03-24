using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Win32;
using Docnet.Core;
using Docnet.Core.Models;
using System.Diagnostics;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using Drawing = System.Drawing;
using System.Drawing.Imaging;

namespace EduSyncAI
{
    public partial class WhiteboardWindow : Window
    {
        private enum DrawingMode
        {
            Pen,
            Highlighter,
            Eraser,
            Select,
            Rectangle,
            Circle,
            Line,
            Arrow,
            Triangle
        }

        private int _sessionId;
        private Button? _activeColorButton;
        private Image? _selectedImage;
        private Point _dragStartPoint;
        private bool _isDraggingImage = false;

        // Shape drawing fields
        private DrawingMode _currentMode = DrawingMode.Pen;
        private Point _shapeStartPoint;
        private Shape? _currentShape;
        private bool _isDrawingShape = false;

        // Document viewer state
        private List<BitmapImage> _documentPages = new List<BitmapImage>();
        private int _currentPageIndex = 0;
        private string _currentDocName = "";
        private Image? _docImage = null;

        // ==================== SESSION CONTEXT ====================
        private SessionManagementViewModel? _viewModel;
        private Action? _onSessionEnded;

        // Facial Recognition state
        private readonly GeminiFaceRecognitionService _faceService;
        private readonly DatabaseService _dbService;
        private DispatcherTimer? _cameraTimer;
        private DispatcherTimer? _recognitionTimer;
        private VideoCapture? _capture;
        private bool _isCameraActive = false;
        private bool _isRecognizing = false;
        private HashSet<int> _markedStudents = new HashSet<int>();

        // Attendance UI elements (created dynamically)
        private Image? _attendanceCameraPreview;
        private TextBlock? _attendanceStatusText;
        private StackPanel? _recognizedStudentsList;
        private Border? _recognizedStudentsPanel;
        private Button? _startRecognitionBtn;
        private Button? _stopRecognitionBtn;

        // Live streaming state
        private bool _isStreamingFrames = false;
        private int _liveSessionId = 0;
        private int _frameCounter = 0;
        private static readonly HttpClient _streamClient = new HttpClient();
        private bool _isLiveBroadcasting = false;

        // Jitsi-integrated face recognition
        private DispatcherTimer? _jitsiRecognitionTimer;
        private bool _isJitsiRecognizing = false;
        private HashSet<int> _jitsiMarkedStudents = new HashSet<int>();
        private List<FaceMatch> _jitsiRecognizedList = new List<FaceMatch>();
        private TextBlock? _liveAttendanceStatus;

        // PiP camera overlay
        private bool _isPipActive = false;
        private DispatcherTimer? _pipTimer;
        private Point _pipDragStart;
        private bool _isPipDragging = false;

        // Session timer
        private DispatcherTimer? _sessionTimer;
        private DateTime _sessionStartTime;

        // Active overlay tracking
        private string _activeOverlay = ""; // "attendance", "live", or ""

        public WhiteboardWindow(int sessionId, SessionManagementViewModel? viewModel = null, Action? onSessionEnded = null)
        {
            InitializeComponent();
            _sessionId = sessionId;
            _viewModel = viewModel;
            _onSessionEnded = onSessionEnded;
            _faceService = new GeminiFaceRecognitionService();
            _dbService = new DatabaseService();
            InitializeCanvas();
            
            // Create image library directory
            var imageLibraryDir = System.IO.Path.Combine(AppConfig.DataDir, "WhiteboardImages");
            Directory.CreateDirectory(imageLibraryDir);

            // Start session timer display
            _sessionStartTime = DateTime.Now;
            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sessionTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _sessionStartTime;
                SidebarTimerText.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            };
            _sessionTimer.Start();

            // Show REC indicator if recording is happening (it's started by SessionManagementView)
            SidebarRecBorder.Visibility = Visibility.Visible;
            StartSidebarRecBlink();

            Closing += WhiteboardWindow_Closing;
        }

        private void WhiteboardWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop camera if active
            if (_isCameraActive)
            {
                StopCameraCleanup();
            }
            // Stop live if broadcasting
            if (_isLiveBroadcasting)
            {
                _ = StopLiveBroadcastAsync();
            }
            _sessionTimer?.Stop();
        }

        // ==================== EXISTING WHITEBOARD LOGIC ====================

        private void InitializeCanvas()
        {
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Ink;
            WhiteboardCanvas.DefaultDrawingAttributes.Color = Colors.Black;
            WhiteboardCanvas.DefaultDrawingAttributes.Width = 5;
            WhiteboardCanvas.DefaultDrawingAttributes.Height = 5;
            WhiteboardCanvas.DefaultDrawingAttributes.FitToCurve = true;
            WhiteboardCanvas.DefaultDrawingAttributes.IgnorePressure = false;
        }

        private void PenButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Pen;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Ink;
            WhiteboardCanvas.DefaultDrawingAttributes.IsHighlighter = false;
            UpdateToolButtonStyles(PenButton);
        }

        private void HighlighterButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Highlighter;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Ink;
            WhiteboardCanvas.DefaultDrawingAttributes.IsHighlighter = true;
            UpdateToolButtonStyles(HighlighterButton);
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Eraser;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            UpdateToolButtonStyles(EraserButton);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Select;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Select;
            UpdateToolButtonStyles(SelectButton);
            // Select/Move mode activated silently
        }

        private void UpdateToolButtonStyles(Button activeButton)
        {
            PenButton.Style = (Style)FindResource("ToolButtonStyle");
            HighlighterButton.Style = (Style)FindResource("ToolButtonStyle");
            EraserButton.Style = (Style)FindResource("ToolButtonStyle");
            SelectButton.Style = (Style)FindResource("ToolButtonStyle");
            RectangleButton.Style = (Style)FindResource("ToolButtonStyle");
            CircleButton.Style = (Style)FindResource("ToolButtonStyle");
            LineButton.Style = (Style)FindResource("ToolButtonStyle");
            ArrowButton.Style = (Style)FindResource("ToolButtonStyle");
            TriangleButton.Style = (Style)FindResource("ToolButtonStyle");
            activeButton.Style = (Style)FindResource("ActiveToolButtonStyle");
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorName)
            {
                if (_activeColorButton != null)
                {
                    _activeColorButton.BorderThickness = new Thickness(2);
                    _activeColorButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDC3C7"));
                }
                var color = (Color)ColorConverter.ConvertFromString(colorName);
                WhiteboardCanvas.DefaultDrawingAttributes.Color = color;
                button.BorderThickness = new Thickness(3);
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                _activeColorButton = button;
                if (WhiteboardCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
                {
                    PenButton_Click(PenButton, null);
                }
            }
        }

        private void ThicknessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WhiteboardCanvas == null) return;
            if (ThicknessComboBox.SelectedItem is ComboBoxItem item && item.Tag is string thickness)
            {
                var size = double.Parse(thickness);
                WhiteboardCanvas.DefaultDrawingAttributes.Width = size;
                WhiteboardCanvas.DefaultDrawingAttributes.Height = size;
            }
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(openFileDialog.FileName)),
                        Stretch = Stretch.Uniform,
                        Width = 300,
                        Height = 200,
                        Cursor = Cursors.SizeAll
                    };

                    image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                    image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                    image.MouseMove += Image_MouseMove;

                    var centerX = WhiteboardScrollViewer.HorizontalOffset + (WhiteboardScrollViewer.ViewportWidth / 2);
                    var centerY = WhiteboardScrollViewer.VerticalOffset + (WhiteboardScrollViewer.ViewportHeight / 2);
                    
                    if (WhiteboardScrollViewer.ViewportWidth == 0) centerX = WhiteboardCanvas.ActualWidth / 2;
                    if (WhiteboardScrollViewer.ViewportHeight == 0) centerY = WhiteboardCanvas.ActualHeight / 2;

                    var absoluteX = centerX / CanvasScale.ScaleX;
                    var absoluteY = centerY / CanvasScale.ScaleY;

                    var imageLeft = absoluteX - (image.Width / 2);
                    var imageTop = absoluteY - (image.Height / 2);

                    InkCanvas.SetLeft(image, imageLeft);
                    InkCanvas.SetTop(image, imageTop);
                    WhiteboardCanvas.Children.Add(image);
                    SaveToImageLibrary(openFileDialog.FileName);
                    // Image added silently
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Could not load the requested image: {ex.Message}", "Image Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"Error adding image: {ex.Message}");
                }
            }
        }

        private void SaveToImageLibrary(string sourceImagePath)
        {
            try
            {
                var imageLibraryDir = System.IO.Path.Combine(AppConfig.DataDir, "WhiteboardImages");
                var fileName = System.IO.Path.GetFileName(sourceImagePath);
                var destPath = System.IO.Path.Combine(imageLibraryDir, fileName);
                if (!File.Exists(destPath))
                {
                    File.Copy(sourceImagePath, destPath, true);
                }
            }
            catch { }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }
        private void Image_MouseMove(object sender, MouseEventArgs e) { }
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear without confirmation dialog
            WhiteboardCanvas.Strokes.Clear();
            WhiteboardCanvas.Children.Clear();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var whiteboardsDir = System.IO.Path.Combine(AppConfig.DataDir, "Whiteboards");
                Directory.CreateDirectory(whiteboardsDir);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"Session_{_sessionId}_{timestamp}.png";
                var filePath = System.IO.Path.Combine(whiteboardsDir, filename);
                var rtb = new RenderTargetBitmap(
                    (int)WhiteboardCanvas.ActualWidth,
                    (int)WhiteboardCanvas.ActualHeight,
                    96, 96, PixelFormats.Pbgra32);
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var brush = new VisualBrush(WhiteboardCanvas);
                    context.DrawRectangle(Brushes.White, null, new Rect(0, 0, WhiteboardCanvas.ActualWidth, WhiteboardCanvas.ActualHeight));
                    context.DrawRectangle(brush, null, new Rect(0, 0, WhiteboardCanvas.ActualWidth, WhiteboardCanvas.ActualHeight));
                }
                rtb.Render(visual);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using (var stream = File.Create(filePath))
                {
                    encoder.Save(stream);
                }
                _ = UploadWhiteboardAsync(filePath, filename);
                System.Diagnostics.Debug.WriteLine($"Whiteboard saved successfully and synced to cloud! Location: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving whiteboard: {ex.Message}");
            }
        }

        private async Task UploadWhiteboardAsync(string filePath, string fileName)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri($"{AppConfig.ServerUrl}/");
                    using (var content = new MultipartFormDataContent())
                    {
                        var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                        content.Add(fileContent, "file", fileName);
                        var response = await client.PostAsync($"api/materials/session/{_sessionId}", content);
                    }
                }
            }
            catch { }
        }

        // ==================== SHAPE DRAWING ====================

        private void WhiteboardCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode >= DrawingMode.Rectangle && _currentMode <= DrawingMode.Triangle)
            {
                _isDrawingShape = true;
                _shapeStartPoint = e.GetPosition(WhiteboardCanvas);
                _currentShape = CreateShape(_currentMode, _shapeStartPoint, _shapeStartPoint);
                if (_currentShape != null)
                {
                    WhiteboardCanvas.Children.Add(_currentShape);
                }
            }
        }

        private void WhiteboardCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawingShape && _currentShape != null)
            {
                var currentPoint = e.GetPosition(WhiteboardCanvas);
                UpdateShape(_currentShape, _currentMode, _shapeStartPoint, currentPoint);
            }
        }

        private void WhiteboardCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingShape)
            {
                _isDrawingShape = false;
                _currentShape = null;
            }
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Rectangle;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.None;
            UpdateToolButtonStyles(RectangleButton);
        }

        private void CircleButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Circle;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.None;
            UpdateToolButtonStyles(CircleButton);
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Line;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.None;
            UpdateToolButtonStyles(LineButton);
        }

        private void ArrowButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Arrow;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.None;
            UpdateToolButtonStyles(ArrowButton);
        }

        private void TriangleButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Triangle;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.None;
            UpdateToolButtonStyles(TriangleButton);
        }

        private Shape? CreateShape(DrawingMode mode, Point start, Point end)
        {
            var color = WhiteboardCanvas.DefaultDrawingAttributes.Color;
            var thickness = WhiteboardCanvas.DefaultDrawingAttributes.Width;
            switch (mode)
            {
                case DrawingMode.Rectangle:
                    return new System.Windows.Shapes.Rectangle { Stroke = new SolidColorBrush(color), StrokeThickness = thickness, Fill = Brushes.Transparent };
                case DrawingMode.Circle:
                    return new System.Windows.Shapes.Ellipse { Stroke = new SolidColorBrush(color), StrokeThickness = thickness, Fill = Brushes.Transparent };
                case DrawingMode.Line:
                    return new System.Windows.Shapes.Line { Stroke = new SolidColorBrush(color), StrokeThickness = thickness, X1 = start.X, Y1 = start.Y, X2 = end.X, Y2 = end.Y };
                case DrawingMode.Arrow:
                    return CreateArrowShape(start, end, color, thickness);
                case DrawingMode.Triangle:
                    return CreateTriangleShape(start, end, color, thickness);
                default:
                    return null;
            }
        }

        private void UpdateShape(Shape shape, DrawingMode mode, Point start, Point end)
        {
            var left = Math.Min(start.X, end.X);
            var top = Math.Min(start.Y, end.Y);
            var width = Math.Abs(end.X - start.X);
            var height = Math.Abs(end.Y - start.Y);
            switch (mode)
            {
                case DrawingMode.Rectangle:
                case DrawingMode.Circle:
                    InkCanvas.SetLeft(shape, left);
                    InkCanvas.SetTop(shape, top);
                    shape.Width = width;
                    shape.Height = height;
                    break;
                case DrawingMode.Line:
                    if (shape is System.Windows.Shapes.Line line) { line.X2 = end.X; line.Y2 = end.Y; }
                    break;
                case DrawingMode.Arrow:
                    if (shape is System.Windows.Shapes.Polygon arrow) UpdateArrowShape(arrow, start, end);
                    break;
                case DrawingMode.Triangle:
                    if (shape is System.Windows.Shapes.Polygon triangle) UpdateTriangleShape(triangle, start, end);
                    break;
            }
        }

        private System.Windows.Shapes.Polygon CreateArrowShape(Point start, Point end, Color color, double thickness)
        {
            var arrow = new System.Windows.Shapes.Polygon { Stroke = new SolidColorBrush(color), StrokeThickness = thickness, Fill = new SolidColorBrush(color) };
            UpdateArrowShape(arrow, start, end);
            return arrow;
        }

        private void UpdateArrowShape(System.Windows.Shapes.Polygon arrow, Point start, Point end)
        {
            var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            var arrowHeadLength = 15;
            var lineEnd = new Point(end.X - arrowHeadLength * Math.Cos(angle), end.Y - arrowHeadLength * Math.Sin(angle));
            var point1 = end;
            var point2 = new Point(end.X - arrowHeadLength * Math.Cos(angle - Math.PI / 6), end.Y - arrowHeadLength * Math.Sin(angle - Math.PI / 6));
            var point3 = new Point(end.X - arrowHeadLength * Math.Cos(angle + Math.PI / 6), end.Y - arrowHeadLength * Math.Sin(angle + Math.PI / 6));
            arrow.Points.Clear();
            arrow.Points.Add(point1);
            arrow.Points.Add(point2);
            arrow.Points.Add(point3);
            if (arrow.Tag == null)
            {
                var line = new System.Windows.Shapes.Line { Stroke = arrow.Stroke, StrokeThickness = arrow.StrokeThickness, X1 = start.X, Y1 = start.Y, X2 = lineEnd.X, Y2 = lineEnd.Y };
                arrow.Tag = line;
                WhiteboardCanvas.Children.Add(line);
            }
            else if (arrow.Tag is System.Windows.Shapes.Line existingLine)
            {
                existingLine.X1 = start.X; existingLine.Y1 = start.Y;
                existingLine.X2 = lineEnd.X; existingLine.Y2 = lineEnd.Y;
            }
        }

        private System.Windows.Shapes.Polygon CreateTriangleShape(Point start, Point end, Color color, double thickness)
        {
            var triangle = new System.Windows.Shapes.Polygon { Stroke = new SolidColorBrush(color), StrokeThickness = thickness, Fill = Brushes.Transparent };
            UpdateTriangleShape(triangle, start, end);
            return triangle;
        }

        private void UpdateTriangleShape(System.Windows.Shapes.Polygon triangle, Point start, Point end)
        {
            var midX = (start.X + end.X) / 2;
            triangle.Points.Clear();
            triangle.Points.Add(new Point(midX, start.Y));
            triangle.Points.Add(new Point(start.X, end.Y));
            triangle.Points.Add(new Point(end.X, end.Y));
        }

        // ==================== DOCUMENT VIEWER ====================

        private void DocumentButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Open Document",
                Filter = "Documents|*.pdf;*.docx;*.doc|PDF Files|*.pdf|Word Documents|*.docx;*.doc|All Files|*.*",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var ext = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();
                    _currentDocName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    if (ext == ".pdf") LoadPdfDocument(openFileDialog.FileName);
                    else if (ext == ".docx" || ext == ".doc") LoadWordDocument(openFileDialog.FileName);
                    else System.Diagnostics.Debug.WriteLine("Unsupported file format.");
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error opening document: {ex.Message}"); }
            }
        }

        private void LoadPdfDocument(string filePath)
        {
            _documentPages.Clear();
            using (var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(1920, 1080)))
            {
                for (int i = 0; i < docReader.GetPageCount(); i++)
                {
                    using (var pageReader = docReader.GetPageReader(i))
                    {
                        var rawBytes = pageReader.GetImage();
                        var width = pageReader.GetPageWidth();
                        var height = pageReader.GetPageHeight();
                        var bitmapSource = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, rawBytes, width * 4);
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        using (var ms = new MemoryStream())
                        {
                            encoder.Save(ms);
                            ms.Position = 0;
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = ms;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();
                            _documentPages.Add(bitmapImage);
                        }
                    }
                }
            }
            if (_documentPages.Count > 0) { _currentPageIndex = 0; ShowDocumentPage(_currentPageIndex); ShowDocNavBar(); }
            else System.Diagnostics.Debug.WriteLine("Could not load any pages from this PDF.");
        }

        private void LoadWordDocument(string filePath)
        {
            try
            {
                _documentPages.Clear();
                var text = ExtractWordText(filePath);
                var pageImage = RenderTextToImage(text, _currentDocName);
                _documentPages.Add(pageImage);
                _currentPageIndex = 0;
                ShowDocumentPage(_currentPageIndex);
                ShowDocNavBar();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading Word document: {ex.Message}"); }
        }

        private string ExtractWordText(string filePath)
        {
            try
            {
                using (var zip = System.IO.Compression.ZipFile.OpenRead(filePath))
                {
                    var docEntry = zip.GetEntry("word/document.xml");
                    if (docEntry != null)
                    {
                        using (var stream = docEntry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            var xml = reader.ReadToEnd();
                            var text = System.Text.RegularExpressions.Regex.Replace(xml, "<[^>]+>", " ");
                            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
                            return text.Length > 3000 ? text.Substring(0, 3000) + "..." : text;
                        }
                    }
                }
            }
            catch { }
            return "Could not extract text from this Word document.";
        }

        private BitmapImage RenderTextToImage(string text, string title)
        {
            var visual = new DrawingVisual();
            var width = 1600; var height = 900;
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
                var titleText = new FormattedText($"📄 {title}", System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), 24, Brushes.Black, 96);
                titleText.MaxTextWidth = width - 100;
                context.DrawText(titleText, new Point(50, 30));
                context.DrawLine(new Pen(Brushes.LightGray, 1), new Point(50, 70), new Point(width - 50, 70));
                var contentText = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 16, Brushes.Black, 96);
                contentText.MaxTextWidth = width - 100;
                contentText.MaxTextHeight = height - 100;
                context.DrawText(contentText, new Point(50, 85));
            }
            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms); ms.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        private void ShowDocumentPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _documentPages.Count) return;
            _currentPageIndex = pageIndex;
            if (_docImage != null && WhiteboardCanvas.Children.Contains(_docImage))
                WhiteboardCanvas.Children.Remove(_docImage);
            var pageSource = _documentPages[pageIndex];
            _docImage = new Image
            {
                Source = pageSource, Stretch = Stretch.Uniform,
                Width = Math.Min(600, WhiteboardCanvas.ActualWidth * 0.6),
                Cursor = Cursors.SizeAll, Tag = "DocPage"
            };
            var aspectRatio = (double)pageSource.PixelHeight / pageSource.PixelWidth;
            _docImage.Height = _docImage.Width * aspectRatio;
            double currentScrollX = WhiteboardScrollViewer.HorizontalOffset;
            double currentScrollY = WhiteboardScrollViewer.VerticalOffset;
            double viewportWidth = WhiteboardScrollViewer.ViewportWidth;
            double viewportHeight = WhiteboardScrollViewer.ViewportHeight;
            
            if (viewportWidth == 0) viewportWidth = Math.Min(800, WhiteboardCanvas.ActualWidth);
            if (viewportHeight == 0) viewportHeight = Math.Min(600, WhiteboardCanvas.ActualHeight);

            double unscaledViewportCenterX = (currentScrollX + viewportWidth / 2) / CanvasScale.ScaleX;
            double unscaledViewportCenterY = (currentScrollY + viewportHeight / 2) / CanvasScale.ScaleY;

            var centerX = Math.Max(0, unscaledViewportCenterX - (_docImage.Width / 2));
            var centerY = Math.Max(0, unscaledViewportCenterY - (_docImage.Height / 2));
            InkCanvas.SetLeft(_docImage, centerX);
            InkCanvas.SetTop(_docImage, centerY);
            WhiteboardCanvas.Children.Add(_docImage);
            PageIndicator.Text = $"Page {pageIndex + 1} of {_documentPages.Count}";
            PrevPageBtn.IsEnabled = pageIndex > 0;
            NextPageBtn.IsEnabled = pageIndex < _documentPages.Count - 1;
        }

        private void DoZoom(double targetScale, Point centerOfZoomScreenPos)
        {
            if (targetScale == CanvasScale.ScaleX) return;

            Point centerOfZoomCanvasPos = WhiteboardScrollViewer.TranslatePoint(centerOfZoomScreenPos, WhiteboardCanvas);

            CanvasScale.ScaleX = targetScale;
            CanvasScale.ScaleY = targetScale;
            
            WhiteboardScrollViewer.UpdateLayout();

            Point newScreenPos = WhiteboardCanvas.TranslatePoint(centerOfZoomCanvasPos, WhiteboardScrollViewer);
            
            double dx = newScreenPos.X - centerOfZoomScreenPos.X;
            double dy = newScreenPos.Y - centerOfZoomScreenPos.Y;

            WhiteboardScrollViewer.ScrollToHorizontalOffset(WhiteboardScrollViewer.HorizontalOffset + dx);
            WhiteboardScrollViewer.ScrollToVerticalOffset(WhiteboardScrollViewer.VerticalOffset + dy);
        }

        private void WhiteboardScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
                double newScale = CanvasScale.ScaleX + zoomDelta;
                newScale = Math.Max(0.3, Math.Min(4.0, newScale));
                
                Point mousePos = e.GetPosition(WhiteboardScrollViewer);
                DoZoom(newScale, mousePos);
                
                e.Handled = true;
            }
        }

        private void WhiteboardScrollViewer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.DeltaManipulation.Scale.X != 1.0 || e.DeltaManipulation.Scale.Y != 1.0)
            {
                double newScale = CanvasScale.ScaleX * e.DeltaManipulation.Scale.X;
                newScale = Math.Max(0.3, Math.Min(4.0, newScale));
                
                Point manipulationCenter = e.ManipulationOrigin;
                DoZoom(newScale, manipulationCenter);
                
                e.Handled = true;
            }
        }

        private void ShowDocNavBar()
        {
            DocNavBar.Visibility = Visibility.Visible;
            DocNameText.Text = _currentDocName;
            _currentMode = DrawingMode.Select;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Select;
            UpdateToolButtonStyles(SelectButton);
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e) { if (_currentPageIndex > 0) ShowDocumentPage(_currentPageIndex - 1); }
        private void NextPage_Click(object sender, RoutedEventArgs e) { if (_currentPageIndex < _documentPages.Count - 1) ShowDocumentPage(_currentPageIndex + 1); }

        private void CloseDocument_Click(object sender, RoutedEventArgs e)
        {
            if (_docImage != null && WhiteboardCanvas.Children.Contains(_docImage))
                WhiteboardCanvas.Children.Remove(_docImage);
            _docImage = null;
            _documentPages.Clear();
            _currentPageIndex = 0;
            _currentDocName = "";
            DocNavBar.Visibility = Visibility.Collapsed;
        }

        // ==================== SIDEBAR ACTIONS ====================

        private void SidebarGoLive_Click(object sender, RoutedEventArgs e)
        {
            if (_isLiveBroadcasting)
            {
                // Already broadcasting — show the live overlay and hide PiP
                StopPip();
                LiveOverlayPanel.Visibility = Visibility.Visible;
                return;
            }
            _ = StartLiveBroadcastAsync();
        }

        private async Task StartLiveBroadcastAsync()
        {
            try
            {
                if (_viewModel?.ActiveSession == null)
                {
                    System.Diagnostics.Debug.WriteLine("No active session.");
                    return;
                }

                var authService = new AuthenticationService();
                var lecturer = authService.GetCurrentLecturer();
                var lecturerName = lecturer?.FullName ?? "Lecturer";
                var lecturerId = lecturer?.Id ?? 0;
                var courseName = _viewModel.ActiveSession.CourseName ?? "Class";

                LiveOverlayPanel.Visibility = Visibility.Visible;
                _isLiveBroadcasting = true;

                // Update sidebar button appearance
                SidebarGoLiveBtn.Style = (Style)FindResource("ActiveSidebarButtonStyle");

                await LivePanel.StartBroadcastAsync(
                    _viewModel.ActiveSession.Id,
                    lecturerName,
                    courseName,
                    lecturerId);

                // Enable PiP frame streaming if camera is running
                _liveSessionId = _viewModel.ActiveSession.Id;
                _isStreamingFrames = _isCameraActive;

                // Auto-start facial recognition via Jitsi camera
                StartJitsiRecognition();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting broadcast: {ex.Message}");
                LiveOverlayPanel.Visibility = Visibility.Collapsed;
                _isLiveBroadcasting = false;
                SidebarGoLiveBtn.Style = (Style)FindResource("SidebarButtonStyle");
            }
        }

        private async Task StopLiveBroadcastAsync()
        {
            try
            {
                // Stop PiP and Jitsi facial recognition
                StopPip();
                StopJitsiRecognition();

                _isStreamingFrames = false;
                _isLiveBroadcasting = false;
                await LivePanel.StopBroadcastAsync();
                LiveOverlayPanel.Visibility = Visibility.Collapsed;
                SidebarGoLiveBtn.Style = (Style)FindResource("SidebarButtonStyle");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping broadcast: {ex.Message}");
            }
        }

        private void MinimizeLiveOverlay_Click(object sender, RoutedEventArgs e)
        {
            LiveOverlayPanel.Visibility = Visibility.Collapsed;
            // Broadcast continues in background — show PiP so lecturer is visible on screen
            if (_isLiveBroadcasting)
            {
                StartPip();
            }
        }

        private async void StopLiveOverlay_Click(object sender, RoutedEventArgs e)
        {
            await StopLiveBroadcastAsync();
        }

        // ==================== PiP CAMERA OVERLAY ====================

        private bool _isPipCapturing = false;

        private void StartPip()
        {
            if (_isPipActive) return;
            _isPipActive = true;
            PipOverlay.Visibility = Visibility.Visible;

            _pipTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _pipTimer.Tick += async (s, args) =>
            {
                if (_isPipCapturing) return; // Skip if previous capture still running
                _isPipCapturing = true;
                try { await UpdatePipFrame(); }
                finally { _isPipCapturing = false; }
            };
            _pipTimer.Start();

            // Capture first frame immediately
            _ = UpdatePipFrame();
        }

        private void StopPip()
        {
            if (!_isPipActive) return;
            _isPipActive = false;

            _pipTimer?.Stop();
            _pipTimer = null;

            PipOverlay.Visibility = Visibility.Collapsed;
            PipCameraImage.Source = null;

            // Reset position so it starts at default corner next time
            PipTranslate.X = 0;
            PipTranslate.Y = 0;
        }

        private void ClosePip_Click(object sender, RoutedEventArgs e)
        {
            // User manually hides PiP — broadcast continues, just no preview
            StopPip();
        }

        private void PipHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                _isPipDragging = true;
                _pipDragStart = e.GetPosition(this);
                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void PipHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPipDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(this);
                PipTranslate.X += current.X - _pipDragStart.X;
                PipTranslate.Y += current.Y - _pipDragStart.Y;
                _pipDragStart = current;
                e.Handled = true;
            }
        }

        private void PipHeader_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPipDragging && sender is FrameworkElement element)
            {
                _isPipDragging = false;
                element.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private async Task UpdatePipFrame()
        {
            if (!_isPipActive || !_isLiveBroadcasting) return;

            try
            {
                var base64Data = await LivePanel.CaptureFrameFromJitsiAsync();
                if (base64Data == null) return;

                // Strip the data URI prefix: "data:image/jpeg;base64,..."
                var commaIndex = base64Data.IndexOf(',');
                if (commaIndex < 0) return;
                var rawBase64 = base64Data.Substring(commaIndex + 1);

                var imageBytes = Convert.FromBase64String(rawBase64);
                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 260; // Small PiP, no need for full res
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                Dispatcher.Invoke(() =>
                {
                    if (_isPipActive)
                    {
                        PipCameraImage.Source = bitmap;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PiP] Frame update error: {ex.Message}");
            }
        }

        // ==================== ATTENDANCE OVERLAY ====================

        private void SidebarAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (_activeOverlay == "attendance")
            {
                // Toggle off
                CloseAttendanceOverlay();
                return;
            }
            ShowAttendanceOverlay();
        }

        private void ShowAttendanceOverlay()
        {
            _activeOverlay = "attendance";
            OverlayTitle.Text = "📷 Facial Recognition Attendance";
            OverlayContent.Children.Clear();
            SidebarAttendanceBtn.Style = (Style)FindResource("ActiveSidebarButtonStyle");

            var mainStack = new StackPanel();

            if (_isLiveBroadcasting || _jitsiRecognizedList.Count > 0)
            {
                // ================= LIVE BROADCAST ATTENDANCE VIEW =================
                OverlayTitle.Text = "📝 Attendance — Recognized Students";

                // Status banner
                var statusBorder = new Border
                {
                    Background = _isLiveBroadcasting
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
                    Padding = new Thickness(15, 12, 15, 12),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 15)
                };
                var statusText = new TextBlock
                {
                    Text = _isLiveBroadcasting
                        ? "🔴 LIVE — Facial recognition is running automatically via the live camera. Students are marked as they appear."
                        : _jitsiRecognizedList.Count > 0
                            ? $"✅ Session complete — {_jitsiRecognizedList.Count} student(s) were recognized and marked present."
                            : "ℹ️ Start a Live broadcast to begin automatic facial recognition attendance.",
                    Foreground = Brushes.White,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap
                };
                statusBorder.Child = statusText;
                mainStack.Children.Add(statusBorder);

                // Recognized Students list
                if (_jitsiRecognizedList.Count > 0)
                {
                    var headerText = new TextBlock
                    {
                        Text = $"✅ Recognized Students ({_jitsiRecognizedList.Count})",
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    mainStack.Children.Add(headerText);

                    foreach (var match in _jitsiRecognizedList)
                    {
                        var studentBorder = new Border
                        {
                            Background = Brushes.White,
                            Padding = new Thickness(12),
                            Margin = new Thickness(0, 0, 0, 5),
                            CornerRadius = new CornerRadius(5),
                            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                            BorderThickness = new Thickness(1)
                        };
                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var nameText = new TextBlock
                        {
                            Text = match.Name,
                            FontWeight = FontWeights.SemiBold,
                            FontSize = 14,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(nameText, 0);

                        var confidencePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                        confidencePanel.Children.Add(new TextBlock
                        {
                            Text = $"{(match.Confidence * 100):F0}%",
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                            FontWeight = FontWeights.Bold,
                            FontSize = 14,
                            Margin = new Thickness(0, 0, 5, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        confidencePanel.Children.Add(new TextBlock
                        {
                            Text = "✓",
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                            FontWeight = FontWeights.Bold,
                            FontSize = 16,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        Grid.SetColumn(confidencePanel, 1);

                        grid.Children.Add(nameText);
                        grid.Children.Add(confidencePanel);
                        studentBorder.Child = grid;
                        mainStack.Children.Add(studentBorder);
                    }
                }
                else
                {
                    var emptyText = new TextBlock
                    {
                        Text = "No students have been recognized yet.\n\nAttendance is running in the background while broadcasting.",
                        FontSize = 14,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 10, 0, 0)
                    };
                    mainStack.Children.Add(emptyText);
                }
            }
            else
            {
                // ================= STANDALONE CAMERA VIEW =================
                // Camera preview
                var cameraBorder = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(5),
                    Height = 280,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
                    Margin = new Thickness(0, 0, 0, 15)
                };
                var cameraGrid = new Grid();
                _attendanceCameraPreview = new Image { Stretch = Stretch.Uniform };
                var placeholderText = new TextBlock
                {
                    Text = "📷 Camera Preview",
                    FontSize = 20,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                cameraGrid.Children.Add(placeholderText);
                cameraGrid.Children.Add(_attendanceCameraPreview);
                cameraBorder.Child = cameraGrid;
                mainStack.Children.Add(cameraBorder);

                // Controls
                var controlsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
                _startRecognitionBtn = new Button
                {
                    Content = "📷 Start Recognition",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 10, 15, 10),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                _startRecognitionBtn.Click += StartAttendanceCamera_Click;

                _stopRecognitionBtn = new Button
                {
                    Content = "⏹ Stop Recognition",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 10, 15, 10),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Visibility = Visibility.Collapsed
                };
                _stopRecognitionBtn.Click += StopAttendanceCamera_Click;

                controlsPanel.Children.Add(_startRecognitionBtn);
                controlsPanel.Children.Add(_stopRecognitionBtn);
                mainStack.Children.Add(controlsPanel);

                // Status text
                _attendanceStatusText = new TextBlock
                {
                    Text = "Click 'Start Recognition' to begin. Students will be automatically marked present when recognized.",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                mainStack.Children.Add(_attendanceStatusText);

                // Recognized Students
                _recognizedStudentsPanel = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECF0F1")),
                    Padding = new Thickness(12),
                    CornerRadius = new CornerRadius(5),
                    Visibility = Visibility.Collapsed
                };
                var recStudentStack = new StackPanel();
                recStudentStack.Children.Add(new TextBlock { Text = "✅ Recognized Students", FontSize = 15, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
                _recognizedStudentsList = new StackPanel();
                recStudentStack.Children.Add(_recognizedStudentsList);
                _recognizedStudentsPanel.Child = recStudentStack;
                mainStack.Children.Add(_recognizedStudentsPanel);
            }

            OverlayContent.Children.Add(mainStack);
            OverlayBackdrop.Visibility = Visibility.Collapsed; // No backdrop — panel sits alongside whiteboard
            OverlayPanel.Visibility = Visibility.Visible;
        }

        private void CloseAttendanceOverlay()
        {
            if (_isCameraActive) StopCameraCleanup();
            _activeOverlay = "";
            OverlayPanel.Visibility = Visibility.Collapsed;
            OverlayBackdrop.Visibility = Visibility.Collapsed;
            SidebarAttendanceBtn.Style = (Style)FindResource("SidebarButtonStyle");
        }

        private void OverlayBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Clicking backdrop minimizes the overlay
            MinimizeOverlay_Click(sender, e);
        }

        private void MinimizeOverlay_Click(object sender, RoutedEventArgs e)
        {
            OverlayPanel.Visibility = Visibility.Collapsed;
            OverlayBackdrop.Visibility = Visibility.Collapsed;
            // Keep the active state — clicking the sidebar button again will re-show
        }

        private void CloseOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (_activeOverlay == "attendance") CloseAttendanceOverlay();
            else
            {
                OverlayPanel.Visibility = Visibility.Collapsed;
                OverlayBackdrop.Visibility = Visibility.Collapsed;
                _activeOverlay = "";
            }
        }
        // ==================== JITSI-INTEGRATED FACIAL RECOGNITION ====================

        private void StartJitsiRecognition()
        {
            if (_viewModel?.ActiveSession == null) return;

            _jitsiMarkedStudents.Clear();
            _jitsiRecognizedList.Clear();
            _isJitsiRecognizing = false;

            // Wait 10 seconds for Jitsi to fully connect before starting recognition
            _jitsiRecognitionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _jitsiRecognitionTimer.Tick += async (s, args) => await AutoRecognizeFacesFromJitsi();
            _jitsiRecognitionTimer.Start();

            Console.WriteLine("[JITSI-FACE] Auto recognition started — snapshots every 15 seconds");
        }

        private void StopJitsiRecognition()
        {
            _jitsiRecognitionTimer?.Stop();
            _jitsiRecognitionTimer = null;
            _isJitsiRecognizing = false;

            var totalMarked = _jitsiMarkedStudents.Count;
            Console.WriteLine($"[JITSI-FACE] Recognition stopped. Total marked: {totalMarked}");

            if (totalMarked > 0)
            {
                MessageBox.Show($"✅ Live attendance session ended!\n\nTotal students marked present via live camera: {totalMarked}",
                    "Attendance Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            _jitsiMarkedStudents.Clear();
        }

        private async Task AutoRecognizeFacesFromJitsi()
        {
            if (_isJitsiRecognizing || !_isLiveBroadcasting) return;
            if (_viewModel?.ActiveSession == null) return;

            try
            {
                _isJitsiRecognizing = true;

                // Capture frame from Jitsi WebView2
                var base64Frame = await LivePanel.CaptureFrameFromJitsiAsync();
                if (string.IsNullOrEmpty(base64Frame))
                {
                    Console.WriteLine("[JITSI-FACE] No frame captured from Jitsi");
                    return;
                }

                Console.WriteLine($"[JITSI-FACE] Frame captured, length={base64Frame.Length}");

                // Capture values for background thread
                var sessionId = _viewModel.ActiveSession.Id;
                var currentMarked = new HashSet<int>(_jitsiMarkedStudents);

                // Run ALL async work on background thread
                var bgResult = await Task.Run(async () =>
                {
                    var result = await _faceService.RecognizeFacesAsync(sessionId, base64Frame).ConfigureAwait(false);
                    if (!result.Success)
                        return new { result.Success, result.Error, NewMatches = new List<FaceMatch>(), MarkedCount = 0 };

                    var newMatches = result.Matches.Where(m => !currentMarked.Contains(m.StudentId)).ToList();
                    int markedCount = 0;
                    if (newMatches.Count > 0)
                    {
                        markedCount = await _faceService.MarkAttendanceAsync(sessionId, newMatches).ConfigureAwait(false);
                    }
                    return new { result.Success, result.Error, NewMatches = newMatches, MarkedCount = markedCount };
                });

                // Update UI on dispatcher thread
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!bgResult.Success)
                        {
                            Console.WriteLine($"[JITSI-FACE] Recognition error: {bgResult.Error}");
                            return;
                        }

                        if (bgResult.NewMatches.Count > 0 && bgResult.MarkedCount > 0)
                        {
                            foreach (var match in bgResult.NewMatches)
                            {
                                _jitsiMarkedStudents.Add(match.StudentId);
                                _jitsiRecognizedList.Add(match);
                                Console.WriteLine($"[JITSI-FACE] ✅ Marked: {match.Name} ({(match.Confidence * 100):F0}%)");
                            }

                            System.Media.SystemSounds.Exclamation.Play();

                            if (_viewModel?.ActiveSession != null)
                            {
                                _viewModel.ActiveSession.AttendanceCount += bgResult.MarkedCount;
                                _dbService.UpdateClassSession(_viewModel.ActiveSession);
                            }

                            // Update the Live header attendance badge
                            LiveAttendanceStatusText.Text = $"  |  ✅ {_jitsiMarkedStudents.Count} student(s) marked present";
                        }
                        else
                        {
                            Console.WriteLine($"[JITSI-FACE] Scan complete — {_jitsiMarkedStudents.Count} total marked so far");
                        }
                    }
                    catch (Exception uiEx)
                    {
                        Console.WriteLine($"[JITSI-FACE] UI error: {uiEx.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JITSI-FACE] Error: {ex.Message}");
            }
            finally
            {
                _isJitsiRecognizing = false;
            }
        }

        // ==================== FACIAL RECOGNITION LOGIC ====================

        private void StartAttendanceCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_attendanceStatusText != null) _attendanceStatusText.Text = "Starting native camera...";
                if (_startRecognitionBtn != null) _startRecognitionBtn.Visibility = Visibility.Collapsed;
                if (_stopRecognitionBtn != null) _stopRecognitionBtn.Visibility = Visibility.Visible;

                _capture = new VideoCapture(0);
                if (!_capture.IsOpened)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to open camera.");
                    if (_attendanceStatusText != null) _attendanceStatusText.Text = "Camera failed to start";
                    if (_startRecognitionBtn != null) _startRecognitionBtn.Visibility = Visibility.Visible;
                    if (_stopRecognitionBtn != null) _stopRecognitionBtn.Visibility = Visibility.Collapsed;
                    return;
                }

                _isCameraActive = true;
                _markedStudents.Clear();
                if (_attendanceStatusText != null)
                    _attendanceStatusText.Text = "🔴 LIVE: Native recognition active. Students will be marked as they appear.";

                _cameraTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
                _cameraTimer.Tick += (s, args) => UpdateAttendanceCameraPreview();
                _cameraTimer.Start();

                _recognitionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                _recognitionTimer.Tick += async (s, args) => await AutoRecognizeFaces();
                _recognitionTimer.Start();

                // Enable PiP frame streaming if live
                if (_isLiveBroadcasting)
                {
                    _isStreamingFrames = true;
                }

                System.Diagnostics.Debug.WriteLine("Native recognition started!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting camera: {ex.Message}");
                if (_startRecognitionBtn != null) _startRecognitionBtn.Visibility = Visibility.Visible;
                if (_stopRecognitionBtn != null) _stopRecognitionBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void StopAttendanceCamera_Click(object sender, RoutedEventArgs e)
        {
            StopCameraCleanup();
        }

        private void StopCameraCleanup()
        {
            try
            {
                _cameraTimer?.Stop(); _cameraTimer = null;
                _recognitionTimer?.Stop(); _recognitionTimer = null;
                _isCameraActive = false;
                _isRecognizing = false;
                _isStreamingFrames = false;

                if (_capture != null) { _capture.Dispose(); _capture = null; }
                if (_attendanceCameraPreview != null) _attendanceCameraPreview.Source = null;
                if (_startRecognitionBtn != null) _startRecognitionBtn.Visibility = Visibility.Visible;
                if (_stopRecognitionBtn != null) _stopRecognitionBtn.Visibility = Visibility.Collapsed;

                var totalMarked = _markedStudents.Count;
                if (_attendanceStatusText != null)
                    _attendanceStatusText.Text = $"Recognition stopped. Total students marked: {totalMarked}";

                if (totalMarked > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Recognition session ended! Total marked: {totalMarked}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping camera: {ex.Message}");
            }
        }

        private void UpdateAttendanceCameraPreview()
        {
            try
            {
                if (_capture == null || !_capture.IsOpened) return;
                using (var frame = _capture.QueryFrame())
                {
                    if (frame != null)
                    {
                        if (_attendanceCameraPreview != null)
                            _attendanceCameraPreview.Source = MatToBitmapSource(frame);

                        // PiP streaming
                        _frameCounter++;
                        if (_isStreamingFrames && _frameCounter % 6 == 0)
                        {
                            _ = SendFrameAsync(frame);
                        }
                    }
                }
            }
            catch { }
        }

        private async Task AutoRecognizeFaces()
        {
            if (_isRecognizing || _capture == null || !_capture.IsOpened) return;
            try
            {
                _isRecognizing = true;
                if (_viewModel?.ActiveSession == null) return;
                if (_attendanceStatusText != null) _attendanceStatusText.Text = "📸 Taking snapshot for recognition...";

                string base64Snapshot = "";
                using (var frame = _capture.QueryFrame())
                {
                    if (frame == null) return;
                    using (var mem = new MemoryStream())
                    {
                        using (Drawing.Bitmap bitmap = frame.ToBitmap())
                        {
                            bitmap.Save(mem, ImageFormat.Jpeg);
                            base64Snapshot = Convert.ToBase64String(mem.ToArray());
                        }
                    }
                }
                if (string.IsNullOrEmpty(base64Snapshot)) return;
                if (_attendanceStatusText != null) _attendanceStatusText.Text = "🔍 Matching faces against profile pictures...";

                // Capture values needed by background thread
                var sessionId = _viewModel.ActiveSession.Id;
                var currentMarked = new HashSet<int>(_markedStudents);

                // Run ALL async work on background thread — no async calls in UI callbacks
                var bgResult = await Task.Run(async () =>
                {
                    var result = await _faceService.RecognizeFacesAsync(sessionId, base64Snapshot).ConfigureAwait(false);
                    if (!result.Success)
                        return new { result.Success, result.Error, NewMatches = new List<FaceMatch>(), MarkedCount = 0 };

                    var newMatches = result.Matches.Where(m => !currentMarked.Contains(m.StudentId)).ToList();
                    int markedCount = 0;
                    try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppConfig.DataDir, "face_debug.log"), $"[{DateTime.Now:HH:mm:ss.fff}] [BG] Matches={result.Matches.Count}, NewMatches={newMatches.Count}, currentMarked={currentMarked.Count}\n"); } catch { }
                    if (newMatches.Count > 0)
                    {
                        markedCount = await _faceService.MarkAttendanceAsync(sessionId, newMatches).ConfigureAwait(false);
                    }
                    try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppConfig.DataDir, "face_debug.log"), $"[{DateTime.Now:HH:mm:ss.fff}] [BG] Final: NewMatches={newMatches.Count}, MarkedCount={markedCount}\n"); } catch { }
                    return new { result.Success, result.Error, NewMatches = newMatches, MarkedCount = markedCount };
                });

                // Now update UI on the dispatcher thread — NO async calls here, only UI element updates
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!bgResult.Success)
                        {
                            if (_attendanceStatusText != null) _attendanceStatusText.Text = $"🔴 LIVE: Recognition error - {bgResult.Error}";
                            return;
                        }

                        if (bgResult.NewMatches.Count > 0 && bgResult.MarkedCount > 0)
                        {
                            foreach (var match in bgResult.NewMatches)
                            {
                                _markedStudents.Add(match.StudentId);
                                if (_recognizedStudentsList != null)
                                {
                                    var studentPanel = new Border
                                    {
                                        Background = Brushes.White,
                                        Padding = new Thickness(10),
                                        Margin = new Thickness(0, 0, 0, 5),
                                        CornerRadius = new CornerRadius(3)
                                    };
                                    var grid = new Grid();
                                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                                    var nameText = new TextBlock { Text = match.Name, FontWeight = FontWeights.SemiBold };
                                    Grid.SetColumn(nameText, 0);
                                    var confidencePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                                    confidencePanel.Children.Add(new TextBlock
                                    {
                                        Text = $"{(match.Confidence * 100):F0}%",
                                        Foreground = Brushes.Green,
                                        FontWeight = FontWeights.Bold,
                                        Margin = new Thickness(0, 0, 5, 0)
                                    });
                                    confidencePanel.Children.Add(new TextBlock { Text = "✓", Foreground = Brushes.Green, FontWeight = FontWeights.Bold });
                                    Grid.SetColumn(confidencePanel, 1);
                                    grid.Children.Add(nameText);
                                    grid.Children.Add(confidencePanel);
                                    studentPanel.Child = grid;
                                    _recognizedStudentsList.Children.Insert(0, studentPanel);
                                }
                            }
                            System.Media.SystemSounds.Exclamation.Play();
                            if (_viewModel?.ActiveSession != null)
                            {
                                _viewModel.ActiveSession.AttendanceCount += bgResult.MarkedCount;
                                _dbService.UpdateClassSession(_viewModel.ActiveSession);
                            }
                            if (_recognizedStudentsPanel != null) _recognizedStudentsPanel.Visibility = Visibility.Visible;
                            if (_attendanceStatusText != null) _attendanceStatusText.Text = $"✅ {_markedStudents.Count} student(s) marked present. Scanning continues...";
                        }
                        else
                        {
                            if (_markedStudents.Count > 0)
                            {
                                if (_attendanceStatusText != null) _attendanceStatusText.Text = $"🔴 LIVE: {_markedStudents.Count} student(s) marked. Scanning for more...";
                            }
                            else
                            {
                                if (_attendanceStatusText != null) _attendanceStatusText.Text = "🔴 LIVE: Scanning... No students recognized yet.";
                            }
                        }
                    }
                    catch (Exception uiEx)
                    {
                        if (_attendanceStatusText != null) _attendanceStatusText.Text = $"⚠️ UI Error: {uiEx.Message}";
                    }
                }));
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_attendanceStatusText != null) _attendanceStatusText.Text = $"⚠️ Error: {ex.Message}. Continuing to scan...";
                }));
            }
            finally
            {
                _isRecognizing = false;
            }
        }

        // ==================== FRAME STREAMING ====================

        private async Task SendFrameAsync(Mat frame)
        {
            try
            {
                using (Drawing.Bitmap bitmap = frame.ToBitmap())
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    var content = new ByteArrayContent(ms.ToArray());
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    await _streamClient.PostAsync($"{AppConfig.ApiUrl}/stream/{_liveSessionId}/frame", content);
                }
            }
            catch { }
        }

        // ==================== END SESSION ====================

        private async void SidebarEndSession_Click(object sender, RoutedEventArgs e)
        {
            // End session without confirmation dialog

            // Save whiteboard first
            SaveButton_Click(sender, e);

            // Stop camera
            if (_isCameraActive) StopCameraCleanup();

            // Stop live broadcast
            if (_isLiveBroadcasting) await StopLiveBroadcastAsync();

            // End session via ViewModel
            if (_viewModel?.EndSessionCommand?.CanExecute(null) == true)
            {
                _viewModel.EndSessionCommand.Execute(null);
            }

            // Invoke callback
            _onSessionEnded?.Invoke();

            // Close the whiteboard
            _sessionTimer?.Stop();
            Close();
        }

        // ==================== HELPERS ====================

        private BitmapSource MatToBitmapSource(Mat mat)
        {
            using (Drawing.Bitmap bitmap = mat.ToBitmap())
            {
                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        // REC blink on sidebar
        private DispatcherTimer? _recBlinkTimer;

        private void StartSidebarRecBlink()
        {
            _recBlinkTimer?.Stop();
            _recBlinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            bool visible = true;
            _recBlinkTimer.Tick += (s, e) =>
            {
                visible = !visible;
                SidebarRecDot.Opacity = visible ? 1.0 : 0.2;
            };
            _recBlinkTimer.Start();
        }
    }
}
