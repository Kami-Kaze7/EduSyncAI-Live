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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using HelixToolkit.Wpf;
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
        private UIElement? _focused2DElement;

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
        private Func<Task>? _onSessionEnded;

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

        public WhiteboardWindow(int sessionId, SessionManagementViewModel? viewModel = null, Func<Task>? onSessionEnded = null)
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

            // Track 3D pen strokes for undo
            Pen3DOverlayCanvas.StrokeCollected += (s, ev) =>
            {
                _undo3DStack.Push(new Undo3DPenStroke(this, ev.Stroke));
            };

            // Initialize the first screen (S1)
            _screens.Add(new ScreenState { Name = "S1" });
            _activeScreenIndex = 0;

            // Pre-configure overlay canvas drawing attributes (ready for when user activates pen)
            Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.None;
            Pen3DOverlayCanvas.DefaultDrawingAttributes.Color = Colors.Black;
            Pen3DOverlayCanvas.DefaultDrawingAttributes.Width = 5;
            Pen3DOverlayCanvas.DefaultDrawingAttributes.Height = 5;
            Pen3DOverlayCanvas.DefaultDrawingAttributes.FitToCurve = true;

            // Default: 3D interact mode — viewport takes mouse events, overlays are visible but non-interactive
            ActivateInteractMode();

            // Focus the 3D viewport on load
            Loaded += (s, ev) =>
            {
                Viewport3DOverlay.Focus();
                UpdateTabBar();
            };
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

        /// <summary>
        /// Restores "3D Interact" mode: viewport receives all mouse events,
        /// overlays are visible (showing existing strokes/shapes) but non-interactive,
        /// and ZIndex is reset so neither overlay blocks the other.
        /// </summary>
        private void ActivateInteractMode()
        {
            // InkCanvas: non-interactive, no background, low ZIndex
            Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.None;
            Pen3DOverlayCanvas.IsHitTestVisible = false;
            Pen3DOverlayCanvas.Background = null;
            Panel.SetZIndex(Pen3DOverlayCanvas, 1);

            // ShapesCanvas: non-interactive (null bg lets child shapes still render), low ZIndex
            Shapes3DOverlayCanvas.Background = null;
            Panel.SetZIndex(Shapes3DOverlayCanvas, 2);

            // Viewport: receives all mouse events
            MainViewport3D.IsHitTestVisible = true;
        }

        private void PenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMode == DrawingMode.Pen && _is3DPenActive == false && MainViewport3D.IsHitTestVisible == false)
            {
                // Toggle OFF -> 3D Interact
                ActivateInteractMode();
                UpdateToolButtonStyles(null);
                Shape3DStatusText.Text = "  |  3D Interact — select, rotate, move shapes";
                return;
            }

            _currentMode = DrawingMode.Pen;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Ink;
            WhiteboardCanvas.DefaultDrawingAttributes.IsHighlighter = false;
            
            // Sync all sticky note ink canvases
            SyncStickyNoteInkMode(InkCanvasEditingMode.Ink, false);

            // Bring InkCanvas to top for pen input
            Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.Ink;
            Pen3DOverlayCanvas.DefaultDrawingAttributes.IsHighlighter = false;
            Pen3DOverlayCanvas.Background = Brushes.Transparent;
            Pen3DOverlayCanvas.IsHitTestVisible = true;
            Panel.SetZIndex(Pen3DOverlayCanvas, 10);
            Panel.SetZIndex(Shapes3DOverlayCanvas, 5);
            Shapes3DOverlayCanvas.Background = null;
            MainViewport3D.IsHitTestVisible = false;
            _is3DPenActive = false;
            _is3DPaintActive = false;
            _is3DSliceActive = false;
            UpdateToolButtonStyles(PenButton);
        }

        private void HighlighterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMode == DrawingMode.Highlighter && MainViewport3D.IsHitTestVisible == false)
            {
                ActivateInteractMode();
                UpdateToolButtonStyles(null);
                Shape3DStatusText.Text = "  |  3D Interact — select, rotate, move shapes";
                return;
            }

            _currentMode = DrawingMode.Highlighter;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Ink;
            WhiteboardCanvas.DefaultDrawingAttributes.IsHighlighter = true;
            
            // Sync all sticky note ink canvases
            SyncStickyNoteInkMode(InkCanvasEditingMode.Ink, true);

            // Bring InkCanvas to top for highlighter input
            Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.Ink;
            Pen3DOverlayCanvas.DefaultDrawingAttributes.IsHighlighter = true;
            Pen3DOverlayCanvas.Background = Brushes.Transparent;
            Pen3DOverlayCanvas.IsHitTestVisible = true;
            Panel.SetZIndex(Pen3DOverlayCanvas, 10);
            Panel.SetZIndex(Shapes3DOverlayCanvas, 5);
            Shapes3DOverlayCanvas.Background = null;
            MainViewport3D.IsHitTestVisible = false;
            _is3DPenActive = false;
            _is3DPaintActive = false;
            _is3DSliceActive = false;
            UpdateToolButtonStyles(HighlighterButton);
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMode == DrawingMode.Eraser && MainViewport3D.IsHitTestVisible == false)
            {
                ActivateInteractMode();
                UpdateToolButtonStyles(null);
                Shape3DStatusText.Text = "  |  3D Interact — select, rotate, move shapes";
                return;
            }

            _currentMode = DrawingMode.Eraser;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;

            // Sync all sticky note ink canvases
            SyncStickyNoteInkMode(InkCanvasEditingMode.EraseByPoint, false);

            // Bring InkCanvas to top for eraser input
            Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            Pen3DOverlayCanvas.Background = Brushes.Transparent;
            Pen3DOverlayCanvas.IsHitTestVisible = true;
            Panel.SetZIndex(Pen3DOverlayCanvas, 10);
            Panel.SetZIndex(Shapes3DOverlayCanvas, 5);
            Shapes3DOverlayCanvas.Background = null;
            MainViewport3D.IsHitTestVisible = false;
            _is3DPenActive = false;
            _is3DPaintActive = false;
            _is3DSliceActive = false;
            UpdateToolButtonStyles(EraserButton);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMode == DrawingMode.Select && MainViewport3D.IsHitTestVisible == false)
            {
                ActivateInteractMode();
                UpdateToolButtonStyles(null);
                Shape3DStatusText.Text = "  |  3D Interact — select, rotate, move shapes";
                return;
            }

            _currentMode = DrawingMode.Select;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Select;

            // Turn off drawing on sticky notes (allow selection instead)
            SyncStickyNoteInkMode(InkCanvasEditingMode.None, false);

            // Bring ShapesCanvas to top for shape/image selection, InkCanvas below for stroke select
            Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.Select;
            Pen3DOverlayCanvas.Background = Brushes.Transparent;
            Pen3DOverlayCanvas.IsHitTestVisible = true;
            Panel.SetZIndex(Shapes3DOverlayCanvas, 10);
            Panel.SetZIndex(Pen3DOverlayCanvas, 5);
            Shapes3DOverlayCanvas.Background = null; // null so clicks pass through to ink select on empty areas
            MainViewport3D.IsHitTestVisible = false;
            _is3DPenActive = false;
            _is3DPaintActive = false;
            _is3DSliceActive = false;
            UpdateToolButtonStyles(SelectButton);
        }

        private void UpdateToolButtonStyles(Button? activeButton)
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
            StickyNoteButton.Style = (Style)FindResource("ToolButtonStyle");

            if (activeButton != null)
            {
                activeButton.Style = (Style)FindResource("ActiveToolButtonStyle");
            }
        }
        
        private void SyncStickyNoteInkMode(InkCanvasEditingMode mode, bool isHighlighter)
        {
            if (StickyNotesCanvas == null) return;
            foreach (UIElement child in StickyNotesCanvas.Children)
            {
                if (child is Border b && b.Tag is InkCanvas ink)
                {
                    ink.EditingMode = mode;
                    ink.DefaultDrawingAttributes.IsHighlighter = isHighlighter;
                }
            }
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
                if (colorName == "Transparent")
                {
                    WhiteboardCanvas.DefaultDrawingAttributes.IsHighlighter = false;
                    WhiteboardCanvas.DefaultDrawingAttributes.Color = Colors.Transparent;
                }
                else
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorName);
                    WhiteboardCanvas.DefaultDrawingAttributes.Color = color;
                }
                button.BorderThickness = new Thickness(3);
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                _activeColorButton = button;
                if (WhiteboardCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
                {
                    PenButton_Click(PenButton, null);
                }
                // Always sync to 3D pen overlay and sticky notes
                Pen3DOverlayCanvas.DefaultDrawingAttributes.Color = WhiteboardCanvas.DefaultDrawingAttributes.Color;
                Pen3DOverlayCanvas.DefaultDrawingAttributes.IsHighlighter = WhiteboardCanvas.DefaultDrawingAttributes.IsHighlighter;
                
                foreach (UIElement child in StickyNotesCanvas.Children)
                {
                    if (child is Border b && b.Tag is InkCanvas ink)
                    {
                        ink.DefaultDrawingAttributes.Color = WhiteboardCanvas.DefaultDrawingAttributes.Color;
                    }
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
                // Always sync to 3D pen overlay and sticky notes
                Pen3DOverlayCanvas.DefaultDrawingAttributes.Width = size;
                Pen3DOverlayCanvas.DefaultDrawingAttributes.Height = size;
                
                foreach (UIElement child in StickyNotesCanvas.Children)
                {
                    if (child is Border b && b.Tag is InkCanvas ink)
                    {
                        ink.DefaultDrawingAttributes.Width = size;
                        ink.DefaultDrawingAttributes.Height = size;
                    }
                }
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

                    // Place in center of the 3D overlay
                    var centerX = (Shapes3DOverlayCanvas.ActualWidth / 2) - (image.Width / 2);
                    var centerY = (Shapes3DOverlayCanvas.ActualHeight / 2) - (image.Height / 2);
                    if (centerX < 0) centerX = 50;
                    if (centerY < 0) centerY = 50;

                    Canvas.SetLeft(image, centerX);
                    Canvas.SetTop(image, centerY);
                    Shapes3DOverlayCanvas.Children.Add(image);

                    // Make draggable and push undo
                    MakeOverlayElementDraggable(image);
                    _undo3DStack.Push(new Undo2DOverlayElement(this, image, "2D Image"));

                    SaveToImageLibrary(openFileDialog.FileName);
                    Shape3DStatusText.Text = "  |  Image added — drag to move";
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Could not load the requested image: {ex.Message}", "Image Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
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


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear all 3D shapes
            foreach (var shape in _placed3DShapes.ToList())
            {
                MainViewport3D.Children.Remove(shape);
                if (shape.Content != null) RemoveMaterials(shape.Content);
            }
            _placed3DShapes.Clear();
            _shapeRotations.Clear();
            _baseScales.Clear();
            _focused3DShape = null;
            _unfocused3DBackgroundColor = (Color)ColorConverter.ConvertFromString("#F0F4F8");
            FocusInfoBadge.Visibility = Visibility.Collapsed;
            RestoreAllMaterialsAndScales();

            // Restore background
            Viewport3DOverlay.Background = new SolidColorBrush(_unfocused3DBackgroundColor);
            GridPlaneVisual.Content.Transform = new ScaleTransform3D(1, 1, 1);

            // Clear ink overlay
            Pen3DOverlayCanvas.Strokes.Clear();

            // Clear shapes overlay
            Shapes3DOverlayCanvas.Children.Clear();

            // Clear sticky notes
            StickyNotesCanvas.Children.Clear();

            // Clear undo stack
            _undo3DStack.Clear();

            Shape3DStatusText.Text = "  |  Screen cleared";
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

        private void ActivateShapeMode(DrawingMode mode, Button button)
        {
            _currentMode = mode;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.None;
            // Bring ShapesCanvas to top with Transparent bg to capture mouse events for shape drawing
            Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.None;
            Pen3DOverlayCanvas.IsHitTestVisible = false;
            Pen3DOverlayCanvas.Background = null;
            Panel.SetZIndex(Shapes3DOverlayCanvas, 10);
            Panel.SetZIndex(Pen3DOverlayCanvas, 5);
            Shapes3DOverlayCanvas.Background = Brushes.Transparent;
            MainViewport3D.IsHitTestVisible = false;
            _is3DPenActive = false;
            _is3DPaintActive = false;
            _is3DSliceActive = false;
            UpdateToolButtonStyles(button);
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e) => ActivateShapeMode(DrawingMode.Rectangle, RectangleButton);
        private void CircleButton_Click(object sender, RoutedEventArgs e) => ActivateShapeMode(DrawingMode.Circle, CircleButton);
        private void LineButton_Click(object sender, RoutedEventArgs e) => ActivateShapeMode(DrawingMode.Line, LineButton);
        private void ArrowButton_Click(object sender, RoutedEventArgs e) => ActivateShapeMode(DrawingMode.Arrow, ArrowButton);
        private void TriangleButton_Click(object sender, RoutedEventArgs e) => ActivateShapeMode(DrawingMode.Triangle, TriangleButton);

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

        // ==================== STICKY NOTES ====================
        
        private void StickyNoteButton_Click(object sender, RoutedEventArgs e)
        {
            // Get lecturer name safely
            string lecturerName = "Lecturer";
            try
            {
                var authService = new AuthenticationService();
                var lecturer = authService.GetCurrentLecturer();
                if (lecturer != null && !string.IsNullOrWhiteSpace(lecturer.FullName))
                {
                    lecturerName = lecturer.FullName;
                }
            }
            catch { }

            var stickyBorder = CreateStickyNote(lecturerName);

            // Center it horizontally, position slightly down from top
            var centerX = (StickyNotesCanvas.ActualWidth / 2) - 100;
            if (centerX < 0 || double.IsNaN(centerX)) centerX = 300;
            Canvas.SetLeft(stickyBorder, centerX);
            Canvas.SetTop(stickyBorder, 100);

            StickyNotesCanvas.Children.Add(stickyBorder);

            // Push to custom undo so we delete from StickyNotesCanvas, not Shapes3DOverlayCanvas
            _undo3DStack.Push(new UndoStickyNoteElement(this, stickyBorder));
            
            Shape3DStatusText.Text = "  |  Sticky note added";
        }

        private Border CreateStickyNote(string authorName)
        {
            // Main container
            var container = new Border
            {
                Width = 200,
                Height = 150,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF176")), // Yellow
                CornerRadius = new CornerRadius(2),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0B359")),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    ShadowDepth = 3,
                    BlurRadius = 8,
                    Opacity = 0.3,
                    Color = Colors.Black
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Body

            // 1. Header (Drag handle + Label)
            var header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33000000")), // Dark overlay
                Cursor = Cursors.SizeAll
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var headerText = new TextBlock
            {
                Text = $" {authorName}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                Opacity = 0.7
            };
            Grid.SetColumn(headerText, 0);
            headerGrid.Children.Add(headerText);

            // Color Palette ComboBox
            var colorPalette = new ComboBox
            {
                Width = 26,
                Height = 18,
                Margin = new Thickness(2, 2, 2, 2),
                Padding = new Thickness(2, 0, 0, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Change Color",
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            
            var colors = new[]
            {
                new { Name = "Yellow", Hex = "#FFF176" },
                new { Name = "Blue", Hex = "#90CAF9" },
                new { Name = "Green", Hex = "#A5D6A7" },
                new { Name = "Orange", Hex = "#FFCC80" },
                new { Name = "Purple", Hex = "#CE93D8" },
                new { Name = "Pink", Hex = "#F48FB1" },
                new { Name = "White", Hex = "#FFFFFF" }
            };

            foreach (var c in colors)
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = 14,
                    Height = 14,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c.Hex)),
                    Margin = new Thickness(2)
                };
                var item = new ComboBoxItem
                {
                    Content = rect,
                    Tag = c.Hex,
                    ToolTip = c.Name
                };
                colorPalette.Items.Add(item);
            }

            colorPalette.SelectionChanged += (s, ev) =>
            {
                if (colorPalette.SelectedItem is ComboBoxItem selected && selected.Tag is string hex)
                {
                    container.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                }
            };

            Grid.SetColumn(colorPalette, 1);
            headerGrid.Children.Add(colorPalette);

            // Delete Button
            var deleteBtn = new Button
            {
                Content = "✕",
                Width = 20,
                Height = 18,
                Margin = new Thickness(2, 2, 4, 2),
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Cursor = Cursors.Hand,
                ToolTip = "Delete Note",
                Opacity = 0.6
            };
            deleteBtn.Click += (s, ev) =>
            {
                StickyNotesCanvas.Children.Remove(container);
                _undo3DStack.Push(new UndoStickyNoteDeleteElement(this, container));
            };
            deleteBtn.MouseEnter += (s, ev) => deleteBtn.Opacity = 1.0;
            deleteBtn.MouseLeave += (s, ev) => deleteBtn.Opacity = 0.6;

            Grid.SetColumn(deleteBtn, 2);
            headerGrid.Children.Add(deleteBtn);

            header.Child = headerGrid;
            Grid.SetRow(header, 0);

            // 2. Body container (TextBox + InkCanvas)
            var bodyGrid = new Grid();
            Grid.SetRow(bodyGrid, 1);

            // 2a. TextBox for typing
            var textBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
                Padding = new Thickness(8),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // 2b. InkCanvas for drawing on the note
            var inkCanvas = new InkCanvas
            {
                Background = Brushes.Transparent,
                EditingMode = (_currentMode == DrawingMode.Pen || _currentMode == DrawingMode.Highlighter) ? InkCanvasEditingMode.Ink : InkCanvasEditingMode.None
            };
            inkCanvas.DefaultDrawingAttributes.Color = WhiteboardCanvas.DefaultDrawingAttributes.Color;
            inkCanvas.DefaultDrawingAttributes.Width = WhiteboardCanvas.DefaultDrawingAttributes.Width;
            inkCanvas.DefaultDrawingAttributes.Height = WhiteboardCanvas.DefaultDrawingAttributes.Height;
            inkCanvas.DefaultDrawingAttributes.IsHighlighter = WhiteboardCanvas.DefaultDrawingAttributes.IsHighlighter;

            // Optional: Tag the border with its ink canvas so tools can toggle it
            container.Tag = inkCanvas;

            bodyGrid.Children.Add(textBox);
            bodyGrid.Children.Add(inkCanvas);

            grid.Children.Add(header);
            grid.Children.Add(bodyGrid);
            container.Child = grid;

            // Context Menu
            BuildStickyNoteContextMenu(container, textBox, headerText);

            // Draggability and Resizing via Header
            MakeStickyNoteDraggable(container, header);

            return container;
        }

        private void BuildStickyNoteContextMenu(Border noteBorder, TextBox noteText, TextBlock headerText)
        {
            var menu = new ContextMenu();

            // Background Colors
            var bgHeader = new MenuItem { Header = "Background Color", IsEnabled = false };
            menu.Items.Add(bgHeader);

            var colors = new[]
            {
                new { Name = "Yellow", Hex = "#FFF176" },
                new { Name = "Blue", Hex = "#90CAF9" },
                new { Name = "Green", Hex = "#A5D6A7" },
                new { Name = "Orange", Hex = "#FFCC80" },
                new { Name = "Purple", Hex = "#CE93D8" },
                new { Name = "Pink", Hex = "#F48FB1" },
                new { Name = "White", Hex = "#FFFFFF" }
            };

            foreach (var c in colors)
            {
                var item = new MenuItem { Header = $"  {c.Name}" };
                item.Click += (s, e) =>
                {
                    noteBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c.Hex));
                };
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());

            // Text Colors
            var textHeader = new MenuItem { Header = "Text Color", IsEnabled = false };
            menu.Items.Add(textHeader);

            var textColors = new[]
            {
                new { Name = "Charcoal", Hex = "#2C3E50" },
                new { Name = "Black", Hex = "#000000" },
                new { Name = "Red", Hex = "#E74C3C" },
            };

            foreach (var tc in textColors)
            {
                var item = new MenuItem { Header = $"  {tc.Name}" };
                item.Click += (s, e) =>
                {
                    noteText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tc.Hex));
                    headerText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tc.Hex));
                };
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());

            // Delete
            var delItem = new MenuItem { Header = "🗑️ Delete Note", Foreground = Brushes.Red };
            delItem.Click += (s, e) =>
            {
                StickyNotesCanvas.Children.Remove(noteBorder);
                // Create custom undo for delete
                _undo3DStack.Push(new UndoStickyNoteDeleteElement(this, noteBorder));
            };
            menu.Items.Add(delItem);

            noteBorder.ContextMenu = menu;
        }

        private void MakeStickyNoteDraggable(Border _element, Border header)
        {
            bool isDragging = false;
            Point dragOffset = new Point();

            // Dragging only on the header
            header.MouseLeftButtonDown += (s, ev) =>
            {
                if (_currentMode == DrawingMode.Eraser) return; // Let eraser just erase ink if we hit InkCanvas

                // Prevent drag if we clicked the dropdown or delete button
                if (ev.OriginalSource is DependencyObject src)
                {
                    var parent = System.Windows.Media.VisualTreeHelper.GetParent(src);
                    while (parent != null)
                    {
                        if (parent is ComboBox || parent is Button) return;
                        parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                    }
                }

                isDragging = true;
                dragOffset = ev.GetPosition(_element);
                header.CaptureMouse();
                ev.Handled = true;

                // Bring to front
                int maxZ = 0;
                foreach (UIElement child in StickyNotesCanvas.Children)
                {
                    int z = Panel.GetZIndex(child);
                    if (z > maxZ) maxZ = z;
                }
                Panel.SetZIndex(_element, maxZ + 1);
            };

            header.MouseMove += (s, ev) =>
            {
                if (isDragging)
                {
                    var pos = ev.GetPosition(StickyNotesCanvas);
                    Canvas.SetLeft(_element, pos.X - dragOffset.X);
                    Canvas.SetTop(_element, pos.Y - dragOffset.Y);
                    ev.Handled = true;
                }
            };

            header.MouseLeftButtonUp += (s, ev) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    header.ReleaseMouseCapture();
                    ev.Handled = true;
                }
            };

            // Resize via MouseWheel (attached to whole element)
            _element.MouseWheel += (s, ev) =>
            {
                if (ev.Delta == 0) return;
                
                // Adjust Width/Height directly instead of ScaleTransform to keep text sharp and flow wrapping correct
                double factor = ev.Delta > 0 ? 1.1 : 0.9;
                
                double newWidth = _element.Width * factor;
                double newHeight = _element.Height * factor;

                if (newWidth > 100 && newWidth < 800)
                {
                    _element.Width = newWidth;
                    _element.Height = newHeight;
                }
                ev.Handled = true;
            };
        }

        // Undo classes for sticky notes
        private class UndoStickyNoteElement : IUndo3DAction
        {
            private readonly WhiteboardWindow _w;
            private readonly UIElement _element;
            public string Description => "Add Sticky Note";
            public UndoStickyNoteElement(WhiteboardWindow w, UIElement element)
            { _w = w; _element = element; }
            public void Undo()
            {
                if (_w.StickyNotesCanvas.Children.Contains(_element))
                    _w.StickyNotesCanvas.Children.Remove(_element);
            }
        }

        private class UndoStickyNoteDeleteElement : IUndo3DAction
        {
            private readonly WhiteboardWindow _w;
            private readonly UIElement _element;
            public string Description => "Delete Sticky Note";
            public UndoStickyNoteDeleteElement(WhiteboardWindow w, UIElement element)
            { _w = w; _element = element; }
            public void Undo()
            {
                if (!_w.StickyNotesCanvas.Children.Contains(_element))
                    _w.StickyNotesCanvas.Children.Add(_element);
            }
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
            if (_docImage != null && Shapes3DOverlayCanvas.Children.Contains(_docImage))
                Shapes3DOverlayCanvas.Children.Remove(_docImage);
            var pageSource = _documentPages[pageIndex];
            _docImage = new Image
            {
                Source = pageSource, Stretch = Stretch.Uniform,
                Width = Math.Min(600, Shapes3DOverlayCanvas.ActualWidth * 0.6),
                Cursor = Cursors.SizeAll, Tag = "DocPage"
            };
            var aspectRatio = (double)pageSource.PixelHeight / pageSource.PixelWidth;
            _docImage.Height = _docImage.Width * aspectRatio;
            
            // Center in viewport
            var viewportWidth = Math.Max(800, Shapes3DOverlayCanvas.ActualWidth);
            var viewportHeight = Math.Max(600, Shapes3DOverlayCanvas.ActualHeight);

            var centerX = Math.Max(0, (viewportWidth - _docImage.Width) / 2);
            var centerY = Math.Max(0, (viewportHeight - _docImage.Height) / 2);
            
            Canvas.SetLeft(_docImage, centerX);
            Canvas.SetTop(_docImage, centerY);
            Shapes3DOverlayCanvas.Children.Add(_docImage);
            
            // Allow user to drag and zoom the document page!
            MakeOverlayElementDraggable(_docImage);

            PageIndicator.Text = $"Page {pageIndex + 1} of {_documentPages.Count}";
            PrevPageBtn.IsEnabled = pageIndex > 0;
            NextPageBtn.IsEnabled = pageIndex < _documentPages.Count - 1;
        }

        private void ShowDocNavBar()
        {
            DocNavBar.Visibility = Visibility.Visible;
            DocNameText.Text = _currentDocName;
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e) { if (_currentPageIndex > 0) ShowDocumentPage(_currentPageIndex - 1); }
        private void NextPage_Click(object sender, RoutedEventArgs e) { if (_currentPageIndex < _documentPages.Count - 1) ShowDocumentPage(_currentPageIndex + 1); }

        private void CloseDocument_Click(object sender, RoutedEventArgs e)
        {
            if (_docImage != null && Shapes3DOverlayCanvas.Children.Contains(_docImage))
                Shapes3DOverlayCanvas.Children.Remove(_docImage);
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

            // Stop recording FIRST — await so recording is fully saved
            // and user sees the "Recording saved!" confirmation MessageBox
            // before the whiteboard closes.
            if (_onSessionEnded != null)
            {
                await _onSessionEnded.Invoke();
            }

            // End session via ViewModel (after recording is saved)
            if (_viewModel?.EndSessionCommand?.CanExecute(null) == true)
            {
                _viewModel.EndSessionCommand.Execute(null);
            }

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

        // ==================== 3D VIEWER ====================

        private enum Shape3DType { None, Cube, Sphere, Cylinder, Pyramid, Cone }

        private bool _is3DMode = true; // Always true — 3D viewport is the default workspace
        private Shape3DType _pendingShape3D = Shape3DType.None;
        private readonly List<ModelVisual3D> _placed3DShapes = new();
        private ModelVisual3D? _focused3DShape = null;
        private readonly Dictionary<GeometryModel3D, Material> _originalMaterials = new();
        private readonly Dictionary<ModelVisual3D, Quaternion> _shapeRotations = new();
        private readonly Dictionary<ModelVisual3D, double> _baseScales = new();  // stores pre-focus scale
        private Helpers.Trackball3D _trackball = new(sensitivity: 1.5);
        
        // 3D Pen state
        private bool _is3DPenActive = false;
        private TubeVisual3D? _current3DStroke = null;
        private ModelVisual3D? _penTarget = null;  // shape being drawn on

        // Shape overlay drawing state (2D shapes on 3D viewport)
        private bool _isDrawingOverlayShape = false;
        private Point _overlayShapeStart;
        private Shape? _currentOverlayShape;

        // ==================== MULTI-SCREEN SYSTEM ====================
        private class ScreenState
        {
            public string Name { get; set; } = "S1";
            public StrokeCollection InkStrokes { get; set; } = new StrokeCollection();
            public List<UIElement> ShapeElements { get; set; } = new List<UIElement>();
            public List<ModelVisual3D> Shapes3D { get; set; } = new List<ModelVisual3D>();
            public Dictionary<GeometryModel3D, Material> Materials { get; set; } = new();
            public Dictionary<ModelVisual3D, Quaternion> Rotations { get; set; } = new();
            public Dictionary<ModelVisual3D, double> Scales { get; set; } = new();
            public Stack<IUndo3DAction> UndoStack { get; set; } = new();
            public Color BackgroundColor { get; set; } = (Color)ColorConverter.ConvertFromString("#F0F4F8");
        }

        private readonly List<ScreenState> _screens = new();
        private int _activeScreenIndex = 0;

        // ==================== 3D UNDO SYSTEM ====================
        private readonly Stack<IUndo3DAction> _undo3DStack = new();

        private interface IUndo3DAction
        {
            void Undo();
            string Description { get; }
        }

        // Undo placing a 3D shape
        private class Undo3DPlace : IUndo3DAction
        {
            private readonly WhiteboardWindow _w;
            private readonly ModelVisual3D _shape;
            public string Description => "Place Shape";
            public Undo3DPlace(WhiteboardWindow w, ModelVisual3D shape) { _w = w; _shape = shape; }
            public void Undo()
            {
                _w.MainViewport3D.Children.Remove(_shape);
                _w._placed3DShapes.Remove(_shape);
                if (_shape.Content != null) _w.RemoveMaterials(_shape.Content);
                _w._baseScales.Remove(_shape);
                _w._shapeRotations.Remove(_shape);
            }
        }

        // Undo painting a model part
        private class Undo3DPaint : IUndo3DAction
        {
            private readonly GeometryModel3D _gm;
            private readonly Material? _oldMat;
            private readonly Material? _oldBackMat;
            public string Description => "Paint";
            public Undo3DPaint(GeometryModel3D gm, Material? oldMat, Material? oldBackMat)
            { _gm = gm; _oldMat = oldMat; _oldBackMat = oldBackMat; }
            public void Undo()
            {
                _gm.Material = _oldMat;
                _gm.BackMaterial = _oldBackMat;
            }
        }

        // Undo slicing — removes the sliced pieces and restores the original shape
        private class Undo3DSlice : IUndo3DAction
        {
            private readonly WhiteboardWindow _w;
            private readonly ModelVisual3D _originalShape;
            public List<ModelVisual3D> NewShapes { get; set; }
            public string Description => "Slice";
            public Undo3DSlice(WhiteboardWindow w, ModelVisual3D originalShape, List<ModelVisual3D> newShapes)
            { _w = w; _originalShape = originalShape; NewShapes = newShapes; }
            public void Undo()
            {
                // Remove sliced pieces
                foreach (var ns in NewShapes)
                {
                    _w.MainViewport3D.Children.Remove(ns);
                    _w._placed3DShapes.Remove(ns);
                    if (ns.Content != null) _w.RemoveMaterials(ns.Content);
                    _w._baseScales.Remove(ns);
                    _w._shapeRotations.Remove(ns);
                }
                // Restore original
                _w.MainViewport3D.Children.Add(_originalShape);
                _w._placed3DShapes.Add(_originalShape);
                _w.StoreMaterials(_originalShape.Content);
                _w._shapeRotations[_originalShape] = Quaternion.Identity;
                _w._baseScales[_originalShape] = 1.0;
            }
        }

        // Undo deleting a shape
        private class Undo3DDelete : IUndo3DAction
        {
            private readonly WhiteboardWindow _w;
            private readonly ModelVisual3D _shape;
            private readonly Quaternion _rotation;
            private readonly double _scale;
            public string Description => "Delete";
            public Undo3DDelete(WhiteboardWindow w, ModelVisual3D shape, Quaternion rotation, double scale)
            { _w = w; _shape = shape; _rotation = rotation; _scale = scale; }
            public void Undo()
            {
                _w.MainViewport3D.Children.Add(_shape);
                _w._placed3DShapes.Add(_shape);
                _w.StoreMaterials(_shape.Content);
                _w._shapeRotations[_shape] = _rotation;
                _w._baseScales[_shape] = _scale;
            }
        }

        // Undo a 3D pen stroke
        private class Undo3DPenStroke : IUndo3DAction
        {
            private readonly WhiteboardWindow _w;
            private readonly System.Windows.Ink.Stroke _stroke;
            public string Description => "3D Pen Stroke";
            public Undo3DPenStroke(WhiteboardWindow w, System.Windows.Ink.Stroke stroke)
            { _w = w; _stroke = stroke; }
            public void Undo()
            {
                if (_w.Pen3DOverlayCanvas.Strokes.Contains(_stroke))
                    _w.Pen3DOverlayCanvas.Strokes.Remove(_stroke);
            }
        }

        private class Undo2DOverlayElement : IUndo3DAction
        {
            private readonly WhiteboardWindow _w;
            private readonly UIElement _element;
            public string Description { get; }
            public Undo2DOverlayElement(WhiteboardWindow w, UIElement element, string desc = "2D Shape")
            { _w = w; _element = element; Description = desc; }
            public void Undo()
            {
                if (_w.Shapes3DOverlayCanvas.Children.Contains(_element))
                    _w.Shapes3DOverlayCanvas.Children.Remove(_element);
            }
        }

        // Undo deleting an overlay element
        private class Undo2DDeleteElement : IUndo3DAction
        {
            private readonly WhiteboardWindow _w;
            private readonly UIElement _element;
            public string Description { get; }
            public Undo2DDeleteElement(WhiteboardWindow w, UIElement element, string desc = "Delete 2D Element")
            { _w = w; _element = element; Description = desc; }
            public void Undo()
            {
                if (!_w.Shapes3DOverlayCanvas.Children.Contains(_element))
                    _w.Shapes3DOverlayCanvas.Children.Add(_element);
            }
        }

        private void Undo3D()
        {
            if (_undo3DStack.Count == 0)
            {
                Shape3DStatusText.Text = "  |  Nothing to undo";
                return;
            }
            var action = _undo3DStack.Pop();
            action.Undo();
            Unfocus3D();
            Shape3DStatusText.Text = $"  |  Undid: {action.Description}";
        }

        private void Undo3D_Click(object sender, RoutedEventArgs e)
        {
            Undo3D();
        }

        // --- Mode Toggle (legacy, no-op) ---
        private void Toggle3D_Click(object sender, RoutedEventArgs e) { }

        // --- Import Model ---
        private void ImportModel_Click(object sender, RoutedEventArgs e)
        {

            var dlg = new OpenFileDialog
            {
                Filter = "3D Models (*.obj;*.stl)|*.obj;*.stl|All files (*.*)|*.*",
                Title = "Import 3D Model"
            };

            if (dlg.ShowDialog() == true)
            {
                Load3DModelFromFile(dlg.FileName, System.IO.Path.GetFileName(dlg.FileName));
            }
        }

        private async void RepositoryButton_Click(object sender, RoutedEventArgs e)
        {
            var browserDlg = new EduSyncAI.Views.RepositoryBrowserWindow
            {
                Owner = this
            };

            if (browserDlg.ShowDialog() == true && browserDlg.SelectedAsset != null)
            {
                Shape3DStatusText.Text = $"  |  Downloading {browserDlg.SelectedAsset.Title}...";
                var repoService = new EduSyncAI.Services.RepositoryService();
                var localFilePath = await repoService.DownloadAndCacheModelAsync(browserDlg.SelectedAsset);

                if (!string.IsNullOrEmpty(localFilePath))
                {
                    Load3DModelFromFile(localFilePath, browserDlg.SelectedAsset.Title);
                }
                else
                {
                    MessageBox.Show("Failed to download model from the repository.", "Repository Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Load3DModelFromFile(string filePath, string displayTitle)
        {
            try
            {
                var importer = new ModelImporter();

                // Override HelixToolkit's default blue material with a neutral gray
                var neutralMat = new MaterialGroup();
                neutralMat.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 200, 200, 200))));
                neutralMat.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 40));
                importer.DefaultMaterial = neutralMat;

                var modelGroup = importer.Load(filePath);

                // Auto-scale and center the model
                var bounds = modelGroup.Bounds;
                if (!bounds.IsEmpty)
                {
                    double maxDim = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));
                    double scaleTarget = 3.0; // Fit inside a 3-unit box
                    double scaleFactor = (maxDim > 0) ? (scaleTarget / maxDim) : 1.0;

                    double cx = bounds.X + bounds.SizeX / 2.0;
                    double cy = bounds.Y + bounds.SizeY / 2.0;
                    double cz = bounds.Z + bounds.SizeZ / 2.0;

                    var localTransform = new Transform3DGroup();
                    localTransform.Children.Add(new TranslateTransform3D(-cx, -cy, -cz));
                    localTransform.Children.Add(new ScaleTransform3D(scaleFactor, scaleFactor, scaleFactor));
                    modelGroup.Transform = localTransform;
                }

                // Fix any remaining HelixToolkit default blue materials on ALL geometry children.
                // HelixToolkit assigns a non-null blue DiffuseMaterial when .mtl files are missing,
                // so a simple null check doesn't catch them. We detect the blue default and replace it.
                FixDefaultBlueMaterials(modelGroup, neutralMat);

                var visual = new ModelVisual3D { Content = modelGroup };
                
                var transform = new Transform3DGroup();
                transform.Children.Add(new ScaleTransform3D(1, 1, 1)); // [0] Scale
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Quaternion.Identity))); // [1] Rotate
                transform.Children.Add(new TranslateTransform3D(0, 0, -3)); // [2] Translate
                
                visual.Transform = transform;

                MainViewport3D.Children.Add(visual);
                _placed3DShapes.Add(visual);
                StoreMaterials(modelGroup);
                _shapeRotations[visual] = Quaternion.Identity;
                _undo3DStack.Push(new Undo3DPlace(this, visual));

                Shape3DStatusText.Text = $"  |  Imported {displayTitle}";
                Viewport3DOverlay.Focus();

                // Auto-return to 3D Interact mode so the user can immediately drag/rotate the new arrival!
                _currentMode = DrawingMode.Pen; // Reset baseline
                ActivateInteractMode();
                UpdateToolButtonStyles(null);
                Shape3DStatusText.Text = $"  |  Imported {displayTitle} — select, rotate, move shapes";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load 3D model: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- 3D Paint Bucket ---
        private bool _is3DPaintActive = false;
        private Color _unfocused3DBackgroundColor = (Color)ColorConverter.ConvertFromString("#F0F4F8");

        private void Paint3D_Click(object sender, RoutedEventArgs e)
        {
            Set3DPaintActive(!_is3DPaintActive);
        }

        private void Set3DPaintActive(bool active)
        {
            _is3DPaintActive = active;
            Paint3DButton.Background = _is3DPaintActive ? new SolidColorBrush(Color.FromArgb(255, 41, 128, 185)) : new SolidColorBrush(Colors.White);
            Paint3DButton.Foreground = _is3DPaintActive ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 41, 128, 185));

            if (_is3DPaintActive)
            {
                if (_is3DPenActive) Set3DPenActive(false);
                if (_is3DSliceActive) Set3DSliceActive(false);
                ActivateInteractMode();
                Shape3DStatusText.Text = "  |  3D PAINT ACTIVE — Click a shape part to colorize it";
                _pendingShape3D = Shape3DType.None;
            }
            else
            {
                Shape3DStatusText.Text = "  |  3D Paint disabled";
            }
        }

        // --- 3D Background ---
        private void BgColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BgColorComboBox != null && BgColorComboBox.SelectedItem is ComboBoxItem item && Viewport3DOverlay != null)
            {
                if (item.Tag is string colorHex)
                {
                    _unfocused3DBackgroundColor = (Color)ColorConverter.ConvertFromString(colorHex);
                    if (_focused3DShape == null)
                    {
                        Viewport3DOverlay.Background = new SolidColorBrush(_unfocused3DBackgroundColor);
                    }
                }
            }
        }

        // --- 3D Pen ---
        private void Pen3D_Click(object sender, RoutedEventArgs e)
        {
            Set3DPenActive(!_is3DPenActive);
        }

        private void Set3DPenActive(bool active)
        {
            _is3DPenActive = active;
            Pen3DButton.Background = _is3DPenActive ? new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)) : new SolidColorBrush(Colors.White);
            Pen3DButton.Foreground = _is3DPenActive ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));
            
            if (_is3DPenActive)
            {
                if (_is3DPaintActive) Set3DPaintActive(false);
                if (_is3DSliceActive) Set3DSliceActive(false);

                // Copy current pen settings to the 3D overlay canvas
                var da = new System.Windows.Ink.DrawingAttributes
                {
                    Color = WhiteboardCanvas.DefaultDrawingAttributes.Color,
                    Width = WhiteboardCanvas.DefaultDrawingAttributes.Width,
                    Height = WhiteboardCanvas.DefaultDrawingAttributes.Height,
                    IsHighlighter = WhiteboardCanvas.DefaultDrawingAttributes.IsHighlighter,
                    StylusTip = WhiteboardCanvas.DefaultDrawingAttributes.StylusTip
                };
                Pen3DOverlayCanvas.DefaultDrawingAttributes = da;
                Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.Ink;
                Pen3DOverlayCanvas.Background = Brushes.Transparent;
                Pen3DOverlayCanvas.IsHitTestVisible = true;
                Panel.SetZIndex(Pen3DOverlayCanvas, 10);
                Panel.SetZIndex(Shapes3DOverlayCanvas, 5);
                Shapes3DOverlayCanvas.Background = null;
                MainViewport3D.IsHitTestVisible = false;
                
                Shape3DStatusText.Text = "  |  3D PEN ACTIVE — Draw annotations over the 3D scene";
                _pendingShape3D = Shape3DType.None;
            }
            else
            {
                Pen3DOverlayCanvas.EditingMode = InkCanvasEditingMode.None;
                ActivateInteractMode();
                Shape3DStatusText.Text = "  |  3D Pen disabled";
            }
        }

        // --- 3D Slice / Detach ---
        private bool _is3DSliceActive = false;

        private void Slice3D_Click(object sender, RoutedEventArgs e)
        {
            Set3DSliceActive(!_is3DSliceActive);
        }

        private void Set3DSliceActive(bool active)
        {
            _is3DSliceActive = active;
            Slice3DButton.Background = _is3DSliceActive ? new SolidColorBrush(Color.FromArgb(255, 142, 68, 173)) : new SolidColorBrush(Colors.White);
            Slice3DButton.Foreground = _is3DSliceActive ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 142, 68, 173));

            if (_is3DSliceActive)
            {
                if (_is3DPaintActive) Set3DPaintActive(false);
                if (_is3DPenActive) Set3DPenActive(false);
                ActivateInteractMode();
                Shape3DStatusText.Text = "  |  3D SLICE ACTIVE — Drag across a model to slice and detach parts!";
                _pendingShape3D = Shape3DType.None;
            }
            else
            {
                Shape3DStatusText.Text = "  |  3D Slice disabled";
            }
        }

        // --- Shape Selectors ---
        private void Cube3D_Click(object sender, RoutedEventArgs e) { Select3DShape(Shape3DType.Cube); }
        private void Sphere3D_Click(object sender, RoutedEventArgs e) { Select3DShape(Shape3DType.Sphere); }
        private void Cylinder3D_Click(object sender, RoutedEventArgs e) { Select3DShape(Shape3DType.Cylinder); }
        private void Pyramid3D_Click(object sender, RoutedEventArgs e) { Select3DShape(Shape3DType.Pyramid); }
        private void Cone3D_Click(object sender, RoutedEventArgs e) { Select3DShape(Shape3DType.Cone); }

        private void Select3DShape(Shape3DType type)
        {
            _pendingShape3D = type;
            Set3DPenActive(false);
            Set3DPaintActive(false);
            Set3DSliceActive(false);
            ActivateInteractMode();
            Shape3DStatusText.Text = $"  |  Click on canvas to place {type}";
            Viewport3DOverlay.Focus();
        }

        // --- Placement ---
        private void Place3DShape(Point clickPoint)
        {
            var model = _pendingShape3D switch
            {
                Shape3DType.Cube => Helpers.Shape3DFactory.CreateCube(1.0),
                Shape3DType.Sphere => Helpers.Shape3DFactory.CreateSphere(0.5, 24),
                Shape3DType.Cylinder => Helpers.Shape3DFactory.CreateCylinder(0.4, 1.0, 24),
                Shape3DType.Pyramid => Helpers.Shape3DFactory.CreatePyramid(1.0, 1.2),
                Shape3DType.Cone => Helpers.Shape3DFactory.CreateCone(0.4, 1.0, 24),
                _ => null
            };
            if (model == null) return;

            // Map screen click to 3D world position (spread shapes across the ground plane)
            double viewW = Viewport3DOverlay.ActualWidth;
            double viewH = Viewport3DOverlay.ActualHeight;
            double nx = (clickPoint.X / viewW - 0.5) * 6.0;  // -3 to +3 range
            double nz = (clickPoint.Y / viewH - 0.5) * -4.0; // map Y to Z depth

            var transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(1, 1, 1)); // [0] Scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Quaternion.Identity))); // [1] Rotate
            transform.Children.Add(new TranslateTransform3D(nx, 0.5, nz)); // [2] Translate

            var visual = new ModelVisual3D { Content = model, Transform = transform };

            MainViewport3D.Children.Add(visual);
            _placed3DShapes.Add(visual);
            StoreMaterials(model);
            _shapeRotations[visual] = Quaternion.Identity;
            _undo3DStack.Push(new Undo3DPlace(this, visual));

            Shape3DStatusText.Text = $"  |  {_pendingShape3D} placed — click it to rotate";
            _pendingShape3D = Shape3DType.None;
        }

        private Point _lastPanPoint;
        private bool _isPanning = false;
        private bool _isSlicingDragging = false;
        private Point _sliceStartPoint;
        private Point _sliceEndPoint;

        private void Viewport3D_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(Viewport3DOverlay);

            if (e.ClickCount == 2)
            {
                Unfocus3D();
                e.Handled = true;
                return;
            }

            if (_is3DSliceActive && e.LeftButton == MouseButtonState.Pressed)
            {
                _isSlicingDragging = true;
                _sliceStartPoint = pos;
                _sliceEndPoint = pos;
                Viewport3DOverlay.CaptureMouse();
                e.Handled = true;
                return;
            }

            // If right click -> pan (works globally, not only when focused)
            if (e.RightButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastPanPoint = pos;
                Viewport3DOverlay.CaptureMouse();
                e.Handled = true;
                return;
            }

            // If we have a pending shape to place, place it
            if (_pendingShape3D != Shape3DType.None)
            {
                Place3DShape(pos);
                e.Handled = true;
                return;
            }

            // Hit-test to find a shape
            var rootShape = HitTest3D(pos);
            var hitRes = VisualTreeHelper.HitTest(MainViewport3D, pos) as RayMeshGeometry3DHitTestResult;

            // If 3D Paint is active
            if (_is3DPaintActive && e.LeftButton == MouseButtonState.Pressed)
            {
                var color = WhiteboardCanvas.DefaultDrawingAttributes.Color;
                if (rootShape != null && hitRes != null && hitRes.ModelHit is GeometryModel3D gm && !(gm is TubeVisual3D))
                {
                    // Save old materials for undo BEFORE changing them
                    var oldMat = gm.Material?.Clone();
                    var oldBackMat = gm.BackMaterial?.Clone();

                    if (color == Colors.Transparent)
                    {
                        if (_originalMaterials.TryGetValue(gm, out var origMat))
                        {
                            gm.Material = origMat;
                            gm.BackMaterial = origMat;
                            Shape3DStatusText.Text = "  |  Shape part material restored";
                        }
                    }
                    else
                    {
                        var mat = new MaterialGroup();
                        mat.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                        mat.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B))));
                        mat.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 40));

                        gm.Material = mat;
                        gm.BackMaterial = mat;
                        _originalMaterials[gm] = mat;

                        Shape3DStatusText.Text = "  |  Shape part painted";
                    }
                    // Push undo for this paint action
                    _undo3DStack.Push(new Undo3DPaint(gm, oldMat, oldBackMat));
                }
                e.Handled = true;
                return;
            }

            if (rootShape != null && hitRes != null)
            {
                Focus3DShape(rootShape);

                // Otherwise only start trackball if using mouse left button
                if (e.StylusDevice == null && e.LeftButton == MouseButtonState.Pressed)
                {
                    _trackball.OnMouseDown(pos);
                    Viewport3DOverlay.CaptureMouse();
                }
            }
            else
            {
                Unfocus3D();
                
                // Start global camera trackball
                if (e.StylusDevice == null && e.LeftButton == MouseButtonState.Pressed && !_isSlicingDragging)
                {
                    _trackball.OnMouseDown(pos);
                    Viewport3DOverlay.CaptureMouse();
                }
            }
            e.Handled = true;
        }

        private void Viewport3D_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(Viewport3DOverlay);

            if (_isSlicingDragging)
            {
                _sliceEndPoint = pos;
                e.Handled = true;
                return;
            }

            if (_focused3DShape == null) return;

            if (_isPanning)
            {
                double dx = pos.X - _lastPanPoint.X;
                double dy = pos.Y - _lastPanPoint.Y;
                
                if (_focused3DShape != null && _focused3DShape.Transform is Transform3DGroup grp && grp.Children.Count >= 3)
                {
                    if (grp.Children[2] is TranslateTransform3D tt)
                    {
                        // Scale movement to viewport — 6 world units across the full viewport width
                        double panScale = 6.0 / Math.Max(1, Viewport3DOverlay.ActualWidth);
                        tt.OffsetX += dx * panScale;
                        tt.OffsetY -= dy * panScale; // Map Y to Y axis
                    }
                }
                else if (_focused3DShape == null)
                {
                    // Global pan (move the camera directly opposite)
                    double panScale = 6.0 / Math.Max(1, Viewport3DOverlay.ActualWidth);
                    var newPos = Camera3D.Position;
                    var right = Vector3D.CrossProduct(Camera3D.LookDirection, Camera3D.UpDirection);
                    right.Normalize();
                    var up = Camera3D.UpDirection;
                    up.Normalize();
                    newPos -= right * (dx * panScale);
                    newPos += up * (dy * panScale);
                    Camera3D.Position = newPos;
                }
                _lastPanPoint = pos;
                e.Handled = true;
                return;
            }

            // Removed 3D Pen execution here, it now uses standard WhiteboardCanvas overlay

            if (_trackball.IsDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var size = new Size(Viewport3DOverlay.ActualWidth, Viewport3DOverlay.ActualHeight);
                var rotation = _trackball.OnMouseMove(pos, size);

                if (rotation != null)
                {
                    if (_focused3DShape != null && _focused3DShape.Transform is Transform3DGroup grp && grp.Children.Count >= 2)
                    {
                        var existingQ = _shapeRotations[_focused3DShape];
                        var newQ = Helpers.Trackball3D.Compose(existingQ, rotation.Quaternion);
                        _shapeRotations[_focused3DShape] = newQ;
                        if (grp.Children[1] is RotateTransform3D rt)
                        {
                            rt.Rotation = new QuaternionRotation3D(newQ);
                        }
                    }
                    else if (_focused3DShape == null)
                    {
                        // Global Camera Rotation
                        if (CameraQuaternion.Quaternion.IsIdentity) CameraQuaternion.Quaternion = Quaternion.Identity;
                        // For camera, invert quaternion to orbit properly
                        var inverseCamRotation = new Quaternion(rotation.Quaternion.Axis, -rotation.Quaternion.Angle);
                        var newCamQ = Helpers.Trackball3D.Compose(CameraQuaternion.Quaternion, inverseCamRotation);
                        CameraQuaternion.Quaternion = newCamQ;
                    }
                }
                e.Handled = true;
            }
        }

        private void Viewport3D_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                Viewport3DOverlay.ReleaseMouseCapture();
                e.Handled = true;
                return;
            }

            if (_isSlicingDragging)
            {
                _isSlicingDragging = false;
                _sliceEndPoint = e.GetPosition(Viewport3DOverlay);
                Viewport3DOverlay.ReleaseMouseCapture();

                // Need enough drag distance to define a meaningful cut line
                double dragLen = ((_sliceEndPoint - _sliceStartPoint)).Length;
                if (dragLen > 20)
                {
                    PerformPlaneSlice(_sliceStartPoint, _sliceEndPoint);
                }
                e.Handled = true;
                return;
            }

            if (_trackball.IsDragging)
            {
                _trackball.OnMouseUp();
                Viewport3DOverlay.ReleaseMouseCapture();
            }
            e.Handled = true;
        }

        // --- True Plane Bisection Slicing ---
        private void PerformPlaneSlice(Point screenStart, Point screenEnd)
        {
            // Build the cutting plane from the two screen points
            var (planePoint, planeNormal) = Helpers.MeshSlicer.BuildCuttingPlane(
                Camera3D, MainViewport3D, screenStart, screenEnd);

            // Find which shapes are under the slice line (check all placed shapes)
            var shapesToSlice = new List<ModelVisual3D>();
            foreach (var shape in _placed3DShapes)
            {
                if (shape == GridPlaneVisual) continue;
                shapesToSlice.Add(shape);
            }

            var newShapes = new List<ModelVisual3D>();
            var removedShapes = new List<ModelVisual3D>();

            foreach (var shape in shapesToSlice)
            {
                var geoModels = Helpers.MeshSlicer.CollectGeometryModelsWithTransform(shape.Content);
                if (geoModels.Count == 0) continue;

                bool anySliced = false;
                var allSideA = new List<GeometryModel3D>();
                var allSideB = new List<GeometryModel3D>();

                foreach (var tuple in geoModels)
                {
                    var gm = tuple.gm;
                    var localTransform = tuple.xform;
                    if (gm.Geometry is not MeshGeometry3D mesh) continue;

                    // Combine visual transform with model transform to get total world transform
                    var totalTransformGroup = new Transform3DGroup();
                    if (localTransform != null && !localTransform.Value.IsIdentity)
                        totalTransformGroup.Children.Add(localTransform);
                    if (shape.Transform != null && !shape.Transform.Value.IsIdentity)
                        totalTransformGroup.Children.Add(shape.Transform);

                    // Bake transform into the mesh vertices — now in world space
                    var worldMesh = Helpers.MeshSlicer.TransformMesh(mesh, totalTransformGroup);

                    // Slice the mesh in world space
                    var result = Helpers.MeshSlicer.SliceMesh(worldMesh, planePoint, planeNormal);

                    if (result.SideA != null && result.SideB != null)
                    {
                        anySliced = true;
                        var matA = gm.Material?.Clone();
                        var matB = gm.Material?.Clone();
                        var backMatA = gm.BackMaterial?.Clone();
                        var backMatB = gm.BackMaterial?.Clone();
                        allSideA.Add(new GeometryModel3D { Geometry = result.SideA, Material = matA, BackMaterial = backMatA });
                        allSideB.Add(new GeometryModel3D { Geometry = result.SideB, Material = matB, BackMaterial = backMatB });
                    }
                    else
                    {
                        if (result.SideA != null)
                            allSideA.Add(new GeometryModel3D { Geometry = result.SideA, Material = gm.Material?.Clone(), BackMaterial = gm.BackMaterial?.Clone() });
                        else if (result.SideB != null)
                            allSideB.Add(new GeometryModel3D { Geometry = result.SideB, Material = gm.Material?.Clone(), BackMaterial = gm.BackMaterial?.Clone() });
                    }
                }

                if (!anySliced) continue;

                // Record this for undo before modifying state
                _undo3DStack.Push(new Undo3DSlice(this, shape, new List<ModelVisual3D>()));

                removedShapes.Add(shape);

                // --- Build Side A (vertices are already in world space, no centering needed) ---
                if (allSideA.Count > 0)
                {
                    var groupA = new Model3DGroup();
                    foreach (var gm in allSideA) groupA.Children.Add(gm);

                    // Identity transform with a tiny nudge along the plane normal to separate the halves
                    var transformA = new Transform3DGroup();
                    transformA.Children.Add(new ScaleTransform3D(1, 1, 1));
                    transformA.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Quaternion.Identity)));
                    transformA.Children.Add(new TranslateTransform3D(
                        planeNormal.X * 0.12,
                        planeNormal.Y * 0.12,
                        planeNormal.Z * 0.12));

                    var visualA = new ModelVisual3D { Content = groupA, Transform = transformA };
                    newShapes.Add(visualA);
                    StoreMaterials(groupA);
                    _baseScales[visualA] = 1.0;
                    _shapeRotations[visualA] = Quaternion.Identity;
                }

                // --- Build Side B ---
                if (allSideB.Count > 0)
                {
                    var groupB = new Model3DGroup();
                    foreach (var gm in allSideB) groupB.Children.Add(gm);

                    var transformB = new Transform3DGroup();
                    transformB.Children.Add(new ScaleTransform3D(1, 1, 1));
                    transformB.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Quaternion.Identity)));
                    transformB.Children.Add(new TranslateTransform3D(
                        -planeNormal.X * 0.12,
                        -planeNormal.Y * 0.12,
                        -planeNormal.Z * 0.12));

                    var visualB = new ModelVisual3D { Content = groupB, Transform = transformB };
                    newShapes.Add(visualB);
                    StoreMaterials(groupB);
                    _baseScales[visualB] = 1.0;
                    _shapeRotations[visualB] = Quaternion.Identity;
                }
            }

            // Apply changes
            foreach (var removed in removedShapes)
            {
                MainViewport3D.Children.Remove(removed);
                _placed3DShapes.Remove(removed);
                if (removed.Content != null) RemoveMaterials(removed.Content);
                _baseScales.Remove(removed);
                _shapeRotations.Remove(removed);
            }
            foreach (var added in newShapes)
            {
                MainViewport3D.Children.Add(added);
                _placed3DShapes.Add(added);
            }

            // Update the undo entries with references to the new shapes
            if (removedShapes.Count > 0)
            {
                // The top of the undo stack is the slice undo; update its new shapes reference
                if (_undo3DStack.Count > 0 && _undo3DStack.Peek() is Undo3DSlice sliceUndo)
                    sliceUndo.NewShapes = newShapes;
            }

            if (removedShapes.Count > 0)
                Shape3DStatusText.Text = $"  |  ✂️ Sliced {removedShapes.Count} model(s) into {newShapes.Count} pieces!";
            else
                Shape3DStatusText.Text = "  |  No models were intersected by the slice line";
        }

        private Vector3D GetTranslation(Transform3D transform)
        {
            if (transform is Transform3DGroup grp && grp.Children.Count >= 3 && grp.Children[2] is TranslateTransform3D tt)
                return new Vector3D(tt.OffsetX, tt.OffsetY, tt.OffsetZ);
            return new Vector3D(0, 0, 0);
        }

        private void Viewport3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            if (_focused3DShape != null && _focused3DShape.Transform is Transform3DGroup grp && grp.Children.Count >= 3)
            {
                // Scale the focused shape
                if (grp.Children[0] is ScaleTransform3D st)
                {
                    double factor = e.Delta > 0 ? 1.1 : 0.9;
                    st.ScaleX *= factor;
                    st.ScaleY *= factor;
                    st.ScaleZ *= factor;
                    e.Handled = true;
                }
            }
            else
            {
                // Global camera zoom — move camera along its look direction
                double factor = e.Delta > 0 ? 0.3 : -0.3;
                var direction = Camera3D.LookDirection;
                direction.Normalize();

                var newPos = Camera3D.Position + direction * factor;
                
                // Prevent zooming past the 3D grid center (Z=0) which flips the camera perspective, 
                // and prevent zooming too far away.
                if (newPos.Z > 0.2 && newPos.Z < 100.0)
                {
                    Camera3D.Position = newPos;
                }
                e.Handled = true;
            }
        }

        private void Viewport3D_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = Viewport3DOverlay;
            e.Handled = true;
        }

        private void Viewport3D_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (_focused3DShape == null) return;
            if (_focused3DShape.Transform is Transform3DGroup grp && grp.Children.Count >= 3)
            {
                // Scale (Pinch)
                if (grp.Children[0] is ScaleTransform3D st)
                {
                    double scale = e.DeltaManipulation.Scale.X;
                    if (scale > 0 && Math.Abs(scale - 1.0) > 0.01)
                    {
                        st.ScaleX *= scale;
                        st.ScaleY *= scale;
                        st.ScaleZ *= scale;
                    }
                }

                // Translate (Touch drag / multi-touch pan)
                if (grp.Children[2] is TranslateTransform3D tt)
                {
                    double dx = e.DeltaManipulation.Translation.X;
                    double dy = e.DeltaManipulation.Translation.Y;
                    if (Math.Abs(dx) > 0.1 || Math.Abs(dy) > 0.1)
                    {
                        if (e.Manipulators.Count() > 1)
                        {
                            double panScale = 6.0 / Math.Max(1, Viewport3DOverlay.ActualWidth);
                            tt.OffsetX += dx * panScale;
                            tt.OffsetY -= dy * panScale; // Map Y to Y axis
                        }
                        else
                        {
                            // 1 finger -> rotate!
                            double ndx = dx / Viewport3DOverlay.ActualWidth * 2.0 * 1.5;
                            double ndy = dy / Viewport3DOverlay.ActualHeight * 2.0 * 1.5;
                            var axis = new Vector3D(-ndy, ndx, 0);
                            double angle = axis.Length * 180.0;
                            if (angle > 0.001)
                            {
                                axis.Normalize();
                                var deltaQ = new Quaternion(axis, angle);
                                var existingQ = _shapeRotations[_focused3DShape];
                                var newQ = Helpers.Trackball3D.Compose(existingQ, deltaQ);
                                _shapeRotations[_focused3DShape] = newQ;
                                if (grp.Children[1] is RotateTransform3D rt)
                                {
                                    rt.Rotation = new QuaternionRotation3D(newQ);
                                }
                            }
                        }
                    }
                }
            }
            e.Handled = true;
        }

        private void Viewport3D_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // Return to 3D interact mode
                if (_focused3DShape != null)
                    Unfocus3D();
                
                _is3DPenActive = false;
                _is3DPaintActive = false;
                _is3DSliceActive = false;
                _pendingShape3D = Shape3DType.None;
                _currentMode = DrawingMode.Pen;
                ActivateInteractMode();
                Shape3DStatusText.Text = "  |  3D Interact — select, rotate, move shapes";
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                if (_focused3DShape != null)
                {
                    DeleteFocused3D_Click(null, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (_focused2DElement != null && Shapes3DOverlayCanvas.Children.Contains(_focused2DElement))
                {
                    Shapes3DOverlayCanvas.Children.Remove(_focused2DElement);
                    _undo3DStack.Push(new Undo2DDeleteElement(this, _focused2DElement));
                    _focused2DElement = null;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Undo3D();
                e.Handled = true;
            }
        }

        private void DeleteFocused3D_Click(object sender, RoutedEventArgs e)
        {
            if (_focused3DShape != null)
            {
                // Push undo before deleting
                var rotation = _shapeRotations.ContainsKey(_focused3DShape) ? _shapeRotations[_focused3DShape] : Quaternion.Identity;
                var scale = _baseScales.ContainsKey(_focused3DShape) ? _baseScales[_focused3DShape] : 1.0;
                _undo3DStack.Push(new Undo3DDelete(this, _focused3DShape, rotation, scale));

                // Delete focused shape
                MainViewport3D.Children.Remove(_focused3DShape);
                _placed3DShapes.Remove(_focused3DShape);
                // Don't call RemoveMaterials — undo needs the materials intact
                _shapeRotations.Remove(_focused3DShape);
                _baseScales.Remove(_focused3DShape);
                _focused3DShape = null;
                FocusInfoBadge.Visibility = Visibility.Collapsed;
                RestoreAllMaterialsAndScales();
                Shape3DStatusText.Text = "  |  Shape deleted (Ctrl+Z to undo)";
                
                // Restore cinematic background blur
                WhiteboardScrollViewer.Effect = null;
                Viewport3DOverlay.Background = new SolidColorBrush(_unfocused3DBackgroundColor);
                GridPlaneVisual.Content.Transform = new ScaleTransform3D(1, 1, 1);
            }
        }

        // --- Hit Testing & Materials ---
        private ModelVisual3D? HitTest3D(Point point)
        {
            var result = VisualTreeHelper.HitTest(MainViewport3D, point);
            if (result is RayMeshGeometry3DHitTestResult meshHit)
            {
                DependencyObject? current = meshHit.VisualHit;
                while (current != null)
                {
                    if (current is ModelVisual3D visual && _placed3DShapes.Contains(visual))
                        return visual;
                    current = VisualTreeHelper.GetParent(current);
                }
            }
            return null;
        }

        private void StoreMaterials(Model3D model)
        {
            if (model is GeometryModel3D gm && gm.Material != null)
            {
                if (!_originalMaterials.ContainsKey(gm))
                    _originalMaterials[gm] = gm.Material;
            }
            else if (model is Model3DGroup mg)
            {
                foreach (var child in mg.Children)
                    StoreMaterials(child);
            }
        }

        private void RemoveMaterials(Model3D model)
        {
            if (model is GeometryModel3D gm)
            {
                _originalMaterials.Remove(gm);
            }
            else if (model is Model3DGroup mg)
            {
                foreach (var child in mg.Children) RemoveMaterials(child);
            }
        }

        /// <summary>
        /// Recursively walks a Model3D tree and replaces HelixToolkit's default blue material
        /// with a neutral replacement. HelixToolkit uses a solid blue DiffuseMaterial as its
        /// fallback when .mtl files are missing — this method detects and replaces those.
        /// </summary>
        private void FixDefaultBlueMaterials(Model3D model, Material replacement)
        {
            if (model is GeometryModel3D gm)
            {
                if (gm.Material == null || IsHelixDefaultBlueMaterial(gm.Material))
                {
                    gm.Material = replacement;
                    gm.BackMaterial = replacement;
                }
            }
            else if (model is Model3DGroup mg)
            {
                foreach (var child in mg.Children)
                    FixDefaultBlueMaterials(child, replacement);
            }
        }

        /// <summary>
        /// Checks whether a material is HelixToolkit's default blue DiffuseMaterial.
        /// The default is a plain DiffuseMaterial with Color = #FF0000FF (pure blue).
        /// </summary>
        private bool IsHelixDefaultBlueMaterial(Material material)
        {
            // Check plain DiffuseMaterial (most common HelixToolkit default)
            if (material is DiffuseMaterial dm && dm.Brush is SolidColorBrush scb)
            {
                var c = scb.Color;
                // HelixToolkit default: pure blue (#0000FF) or near-blue variants
                if (c.R < 30 && c.G < 30 && c.B > 220)
                    return true;
            }

            // Check MaterialGroup where only child is the blue DiffuseMaterial
            if (material is MaterialGroup mg && mg.Children.Count == 1)
            {
                return IsHelixDefaultBlueMaterial(mg.Children[0]);
            }

            return false;
        }

        private void Focus3DShape(ModelVisual3D shape)
        {
            // If re-focusing the same shape, skip
            if (_focused3DShape == shape) return;
            
            _focused3DShape = shape;
            FocusInfoBadge.Visibility = Visibility.Visible;
            if (_is3DPenActive)
                Shape3DStatusText.Text = "  |  3D PEN ACTIVE — Draw on the focused shape";
            else
                Shape3DStatusText.Text = "  |  Drag = Rotate · Right-Click Drag = Move · Scroll/Pinch = Scale";

            // Focus highlighting
            HighlightModel(shape.Content);
        }

        private void HighlightModel(Model3D? model)
        {
            if (model is GeometryModel3D gm && _originalMaterials.ContainsKey(gm))
            {
                var highlightMat = new MaterialGroup();
                highlightMat.Children.Add(_originalMaterials[gm]);
                highlightMat.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(100, 80, 200, 255)))); // cyan glow
                gm.Material = highlightMat;
            }
            else if (model is Model3DGroup mg)
            {
                foreach (var child in mg.Children) HighlightModel(child);
            }
        }

        private void ResetFocusScale(ModelVisual3D shape)
        {
            if (shape.Transform is Transform3DGroup grp && grp.Children.Count >= 1 && grp.Children[0] is ScaleTransform3D st)
            {
                if (_baseScales.TryGetValue(shape, out double baseScale))
                {
                    st.ScaleX = baseScale;
                    st.ScaleY = baseScale;
                    st.ScaleZ = baseScale;
                }
            }
        }

        private void SetMaterialOpacity(Model3D model, double opacity)
        {
            if (model is GeometryModel3D gm && _originalMaterials.ContainsKey(gm))
            {
                gm.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), 128, 128, 128)));
            }
            else if (model is Model3DGroup mg)
            {
                foreach (var child in mg.Children) SetMaterialOpacity(child, opacity);
            }
        }

        private void RestoreMaterial(Model3D model)
        {
            if (model is GeometryModel3D gm && _originalMaterials.TryGetValue(gm, out var mat))
            {
                gm.Material = mat;
            }
            else if (model is Model3DGroup mg)
            {
                foreach (var child in mg.Children) RestoreMaterial(child);
            }
        }

        private void Unfocus3D()
        {
            _focused3DShape = null;
            FocusInfoBadge.Visibility = Visibility.Collapsed;
            RestoreAllMaterialsAndScales();
            Shape3DStatusText.Text = "  |  Ready";
            _trackball.OnMouseUp();
        }

        private void RestoreAllMaterialsAndScales()
        {
            foreach (var s in _placed3DShapes)
            {
                if (s.Content != null) RestoreMaterial(s.Content);
                if (s.Transform is Transform3DGroup grp && grp.Children.Count >= 1 && grp.Children[0] is ScaleTransform3D st)
                {
                    if (_baseScales.TryGetValue(s, out double baseScale))
                    {
                        st.ScaleX = baseScale;
                        st.ScaleY = baseScale;
                        st.ScaleZ = baseScale;
                    }
                }
            }
        }

        // ==================== SHAPES OVERLAY HANDLERS ====================

        private void Shapes3DOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode >= DrawingMode.Rectangle && _currentMode <= DrawingMode.Triangle)
            {
                _isDrawingOverlayShape = true;
                _overlayShapeStart = e.GetPosition(Shapes3DOverlayCanvas);
                _currentOverlayShape = CreateShape(_currentMode, _overlayShapeStart, _overlayShapeStart);
                if (_currentOverlayShape != null)
                {
                    // Give shape a semi-transparent fill so it's hit-testable for dragging later
                    if (_currentOverlayShape.Fill == Brushes.Transparent || _currentOverlayShape.Fill == null)
                        _currentOverlayShape.Fill = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                    Canvas.SetLeft(_currentOverlayShape, _overlayShapeStart.X);
                    Canvas.SetTop(_currentOverlayShape, _overlayShapeStart.Y);
                    Shapes3DOverlayCanvas.Children.Add(_currentOverlayShape);
                }
                e.Handled = true;
            }
        }

        private void Shapes3DOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawingOverlayShape && _currentOverlayShape != null)
            {
                var currentPoint = e.GetPosition(Shapes3DOverlayCanvas);
                UpdateOverlayShape(_currentOverlayShape, _currentMode, _overlayShapeStart, currentPoint);
                e.Handled = true;
            }
        }

        private void Shapes3DOverlay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingOverlayShape && _currentOverlayShape != null)
            {
                // Add drag handlers to the finished shape
                MakeOverlayElementDraggable(_currentOverlayShape);
                // Push undo
                _undo3DStack.Push(new Undo2DOverlayElement(this, _currentOverlayShape, "2D Shape"));

                _isDrawingOverlayShape = false;
                _currentOverlayShape = null;
                e.Handled = true;

                // Return to 3D interact mode after drawing
                Shapes3DOverlayCanvas.Background = null;
            }
        }

        // Make any overlay element (shape or image) draggable and scalable
        private void MakeOverlayElementDraggable(UIElement element)
        {
            bool isDragging = false;
            Point dragOffset = new Point();

            element.MouseLeftButtonDown += (s, ev) =>
            {
                if (_currentMode == DrawingMode.Eraser)
                {
                    Shapes3DOverlayCanvas.Children.Remove(element);
                    _undo3DStack.Push(new Undo2DDeleteElement(this, element));
                    if (_focused2DElement == element) _focused2DElement = null;
                    ev.Handled = true;
                    return;
                }

                // Focus element for keyboard deletion
                _focused2DElement = element;
                
                // Only drag when NOT in shape-drawing mode
                if (_currentMode >= DrawingMode.Rectangle && _currentMode <= DrawingMode.Triangle 
                    && Shapes3DOverlayCanvas.Background != null) return;
                    
                isDragging = true;
                dragOffset = ev.GetPosition(element as FrameworkElement);
                element.CaptureMouse();
                ev.Handled = true;
            };
            element.MouseMove += (s, ev) =>
            {
                if (isDragging)
                {
                    var pos = ev.GetPosition(Shapes3DOverlayCanvas);
                    Canvas.SetLeft(element, pos.X - dragOffset.X);
                    Canvas.SetTop(element, pos.Y - dragOffset.Y);
                    ev.Handled = true;
                }
            };
            element.MouseLeftButtonUp += (s, ev) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    element.ReleaseMouseCapture();
                    ev.Handled = true;
                }
            };

            // Touch Pinch-to-Zoom
            element.IsManipulationEnabled = true;
            element.ManipulationStarting += (s, ev) =>
            {
                if (_currentMode == DrawingMode.Eraser) { ev.Cancel(); return; }
                ev.ManipulationContainer = Shapes3DOverlayCanvas;
                ev.Handled = true;
            };
            element.ManipulationDelta += (s, ev) =>
            {
                if (element.RenderTransform is not ScaleTransform st)
                {
                    st = new ScaleTransform(1.0, 1.0);
                    element.RenderTransform = st;
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                double scale = ev.DeltaManipulation.Scale.X;
                if (scale > 0 && Math.Abs(scale - 1.0) > 0.01)
                {
                    st.ScaleX *= scale;
                    st.ScaleY *= scale;
                }
                ev.Handled = true;
            };

            // Mouse Wheel Zoom
            element.MouseWheel += (s, ev) =>
            {
                if (_currentMode == DrawingMode.Eraser) return;
                
                if (element.RenderTransform is not ScaleTransform st)
                {
                    st = new ScaleTransform(1.0, 1.0);
                    element.RenderTransform = st;
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }
                double factor = ev.Delta > 0 ? 1.1 : 0.9;
                st.ScaleX *= factor;
                st.ScaleY *= factor;
                ev.Handled = true;
            };

            // Set cursor to indicate draggability
            if (element is FrameworkElement fe)
                fe.Cursor = Cursors.SizeAll;
        }

        private void UpdateOverlayShape(Shape shape, DrawingMode mode, Point start, Point end)
        {
            var left = Math.Min(start.X, end.X);
            var top = Math.Min(start.Y, end.Y);
            var width = Math.Abs(end.X - start.X);
            var height = Math.Abs(end.Y - start.Y);
            switch (mode)
            {
                case DrawingMode.Rectangle:
                case DrawingMode.Circle:
                    Canvas.SetLeft(shape, left);
                    Canvas.SetTop(shape, top);
                    shape.Width = width;
                    shape.Height = height;
                    break;
                case DrawingMode.Line:
                    if (shape is System.Windows.Shapes.Line line) { line.X2 = end.X - start.X; line.Y2 = end.Y - start.Y; }
                    break;
                case DrawingMode.Arrow:
                    if (shape is System.Windows.Shapes.Polygon arrow) UpdateArrowShape(arrow, start, end);
                    break;
                case DrawingMode.Triangle:
                    if (shape is System.Windows.Shapes.Polygon triangle) UpdateTriangleShape(triangle, start, end);
                    break;
            }
        }

        // ==================== MULTI-SCREEN SYSTEM ====================

        private void SaveCurrentScreen()
        {
            if (_activeScreenIndex < 0 || _activeScreenIndex >= _screens.Count) return;

            var screen = _screens[_activeScreenIndex];

            // Save ink strokes
            screen.InkStrokes = Pen3DOverlayCanvas.Strokes.Clone();

            // Save shape overlay elements
            screen.ShapeElements.Clear();
            foreach (UIElement child in Shapes3DOverlayCanvas.Children)
                screen.ShapeElements.Add(child);

            // Save 3D shapes
            screen.Shapes3D.Clear();
            screen.Materials.Clear();
            screen.Rotations.Clear();
            screen.Scales.Clear();
            foreach (var shape in _placed3DShapes)
            {
                screen.Shapes3D.Add(shape);
                if (_shapeRotations.ContainsKey(shape))
                    screen.Rotations[shape] = _shapeRotations[shape];
                if (_baseScales.ContainsKey(shape))
                    screen.Scales[shape] = _baseScales[shape];
            }
            // Copy materials
            foreach (var kvp in _originalMaterials)
                screen.Materials[kvp.Key] = kvp.Value;

            // Save undo stack
            screen.UndoStack = new Stack<IUndo3DAction>(_undo3DStack.Reverse());

            // Save background
            screen.BackgroundColor = _unfocused3DBackgroundColor;
        }

        private void LoadScreen(int index)
        {
            if (index < 0 || index >= _screens.Count) return;

            var screen = _screens[index];

            // Load ink strokes
            Pen3DOverlayCanvas.Strokes = screen.InkStrokes.Clone();

            // Load shape overlay elements
            Shapes3DOverlayCanvas.Children.Clear();
            foreach (var elem in screen.ShapeElements)
                Shapes3DOverlayCanvas.Children.Add(elem);

            // Clear current 3D shapes from viewport (don't destroy them — they belong to the old screen)
            foreach (var shape in _placed3DShapes.ToList())
                MainViewport3D.Children.Remove(shape);
            _placed3DShapes.Clear();
            _shapeRotations.Clear();
            _baseScales.Clear();
            _originalMaterials.Clear();
            _focused3DShape = null;
            FocusInfoBadge.Visibility = Visibility.Collapsed;

            // Load 3D shapes
            foreach (var shape in screen.Shapes3D)
            {
                MainViewport3D.Children.Add(shape);
                _placed3DShapes.Add(shape);
                if (screen.Rotations.ContainsKey(shape))
                    _shapeRotations[shape] = screen.Rotations[shape];
                if (screen.Scales.ContainsKey(shape))
                    _baseScales[shape] = screen.Scales[shape];
            }
            foreach (var kvp in screen.Materials)
                _originalMaterials[kvp.Key] = kvp.Value;

            // Load undo stack
            _undo3DStack.Clear();
            foreach (var action in screen.UndoStack)
                _undo3DStack.Push(action);

            // Load background
            _unfocused3DBackgroundColor = screen.BackgroundColor;
            Viewport3DOverlay.Background = new SolidColorBrush(_unfocused3DBackgroundColor);

            _activeScreenIndex = index;
            Shape3DStatusText.Text = $"  |  Screen {screen.Name}";
        }

        private void SwitchScreen(int index)
        {
            if (index == _activeScreenIndex) return;
            SaveCurrentScreen();
            LoadScreen(index);
            UpdateTabBar();
        }

        private void AddScreen_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentScreen();

            var newScreen = new ScreenState
            {
                Name = $"S{_screens.Count + 1}"
            };
            _screens.Add(newScreen);
            LoadScreen(_screens.Count - 1);
            UpdateTabBar();
        }

        private void UpdateTabBar()
        {
            ScreenTabsPanel.Children.Clear();
            for (int i = 0; i < _screens.Count; i++)
            {
                int idx = i; // capture for closure
                var isActive = (i == _activeScreenIndex);

                var tab = new Button
                {
                    Content = _screens[i].Name,
                    Width = 64,
                    Height = 32,
                    Margin = new Thickness(2, 0, 2, 0),
                    Cursor = Cursors.Hand,
                    FontSize = 13,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Background = isActive
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = isActive ? new Thickness(0, 2, 0, 0) : new Thickness(0),
                    BorderBrush = isActive
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5DADE2"))
                        : null
                };

                // Rounded corners
                tab.Resources.Add(typeof(Border), new Style(typeof(Border))
                {
                    Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(4)) }
                });

                tab.Click += (s, ev) => SwitchScreen(idx);
                ScreenTabsPanel.Children.Add(tab);
            }
        }
    }
}
