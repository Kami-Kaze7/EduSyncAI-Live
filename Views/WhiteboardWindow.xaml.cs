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

        public WhiteboardWindow(int sessionId)
        {
            InitializeComponent();
            _sessionId = sessionId;
            InitializeCanvas();
            
            // Create image library directory
            var imageLibraryDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "WhiteboardImages");
            Directory.CreateDirectory(imageLibraryDir);
        }

        private void InitializeCanvas()
        {
            // Set default drawing mode
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Ink;
            
            // Set default pen attributes
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
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            UpdateToolButtonStyles(EraserButton);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = DrawingMode.Select;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Select;
            UpdateToolButtonStyles(SelectButton);
            
            MessageBox.Show("Select/Move mode activated!\n\n" +
                "• Click and drag images to reposition them\n" +
                "• Click and drag strokes to move them\n" +
                "• Click Pen to return to drawing mode",
                "Select Mode", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateToolButtonStyles(Button activeButton)
        {
            // Reset all tool buttons
            PenButton.Style = (Style)FindResource("ToolButtonStyle");
            HighlighterButton.Style = (Style)FindResource("ToolButtonStyle");
            EraserButton.Style = (Style)FindResource("ToolButtonStyle");
            SelectButton.Style = (Style)FindResource("ToolButtonStyle");
            RectangleButton.Style = (Style)FindResource("ToolButtonStyle");
            CircleButton.Style = (Style)FindResource("ToolButtonStyle");
            LineButton.Style = (Style)FindResource("ToolButtonStyle");
            ArrowButton.Style = (Style)FindResource("ToolButtonStyle");
            TriangleButton.Style = (Style)FindResource("ToolButtonStyle");
            
            // Set active button style
            activeButton.Style = (Style)FindResource("ActiveToolButtonStyle");
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorName)
            {
                // Reset previous active color button
                if (_activeColorButton != null)
                {
                    _activeColorButton.BorderThickness = new Thickness(2);
                    _activeColorButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDC3C7"));
                }
                
                // Set new color
                var color = (Color)ColorConverter.ConvertFromString(colorName);
                WhiteboardCanvas.DefaultDrawingAttributes.Color = color;
                
                // Update active color button
                button.BorderThickness = new Thickness(3);
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                _activeColorButton = button;
                
                // Switch to pen mode if in eraser mode
                if (WhiteboardCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke)
                {
                    PenButton_Click(PenButton, null);
                }
            }
        }

        private void ThicknessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Null check - event fires during initialization before canvas is ready
            if (WhiteboardCanvas == null)
                return;
                
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
                    // Create image control
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(openFileDialog.FileName)),
                        Stretch = Stretch.Uniform,
                        Width = 300, // Default width
                        Height = 200, // Default height
                        Cursor = Cursors.SizeAll
                    };

                    // Make image draggable
                    image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                    image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                    image.MouseMove += Image_MouseMove;

                    // Add to canvas at center
                    var centerX = (WhiteboardCanvas.ActualWidth - image.Width) / 2;
                    var centerY = (WhiteboardCanvas.ActualHeight - image.Height) / 2;
                    
                    InkCanvas.SetLeft(image, centerX);
                    InkCanvas.SetTop(image, centerY);
                    
                    WhiteboardCanvas.Children.Add(image);

                    // Save to image library
                    SaveToImageLibrary(openFileDialog.FileName);

                    MessageBox.Show("Image added! Click and drag to reposition.\n\nTip: The image has been saved to your library for future use.",
                        "Image Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding image: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveToImageLibrary(string sourceImagePath)
        {
            try
            {
                var imageLibraryDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "WhiteboardImages");
                var fileName = System.IO.Path.GetFileName(sourceImagePath);
                var destPath = System.IO.Path.Combine(imageLibraryDir, fileName);

                // Copy if not already in library
                if (!File.Exists(destPath))
                {
                    File.Copy(sourceImagePath, destPath, true);
                }
            }
            catch
            {
                // Silently fail - not critical
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Image dragging is now handled by InkCanvas Select mode
            // This handler is kept for future enhancements (e.g., resize, delete)
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            // Image dragging is now handled by InkCanvas Select mode
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Image dragging is now handled by InkCanvas Select mode
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear the entire whiteboard?\n\nThis will remove all drawings and images.\n\nThis action cannot be undone.",
                "Clear Whiteboard",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                WhiteboardCanvas.Strokes.Clear();
                WhiteboardCanvas.Children.Clear(); // Clear images too
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create directory if it doesn't exist
                var whiteboardsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Whiteboards");
                Directory.CreateDirectory(whiteboardsDir);

                // Generate filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"Session_{_sessionId}_{timestamp}.png";
                var filePath = System.IO.Path.Combine(whiteboardsDir, filename);

                // Render InkCanvas to bitmap with white background
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

                // Save as PNG
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (var stream = File.Create(filePath))
                {
                    encoder.Save(stream);
                }

                // Upload to backend
                _ = UploadWhiteboardAsync(filePath, filename);

                MessageBox.Show(
                    $"Whiteboard saved successfully and synced to cloud!\n\nLocation: {filePath}",
                    "Save & Sync Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving whiteboard: {ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            var error = await response.Content.ReadAsStringAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail upload to avoid blocking the UI
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (WhiteboardCanvas.Strokes.Count > 0)
            {
                var result = MessageBox.Show(
                    "Do you want to save the whiteboard before closing?",
                    "Save Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveButton_Click(sender, e);
                    Close();
                }
                else if (result == MessageBoxResult.No)
                {
                    Close();
                }
                // Cancel - do nothing
            }
            else
            {
                Close();
            }
        }

        private void WhiteboardCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode >= DrawingMode.Rectangle && _currentMode <= DrawingMode.Triangle)
            {
                _isDrawingShape = true;
                _shapeStartPoint = e.GetPosition(WhiteboardCanvas);
                
                // Create initial shape (will be updated on mouse move)
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

        // Shape button click handlers
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

        // Shape creation and update methods
        private Shape? CreateShape(DrawingMode mode, Point start, Point end)
        {
            var color = WhiteboardCanvas.DefaultDrawingAttributes.Color;
            var thickness = WhiteboardCanvas.DefaultDrawingAttributes.Width;

            switch (mode)
            {
                case DrawingMode.Rectangle:
                    return new System.Windows.Shapes.Rectangle
                    {
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = thickness,
                        Fill = Brushes.Transparent
                    };

                case DrawingMode.Circle:
                    return new Ellipse
                    {
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = thickness,
                        Fill = Brushes.Transparent
                    };

                case DrawingMode.Line:
                    return new System.Windows.Shapes.Line
                    {
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = thickness,
                        X1 = start.X,
                        Y1 = start.Y,
                        X2 = end.X,
                        Y2 = end.Y
                    };

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
                    if (shape is System.Windows.Shapes.Line line)
                    {
                        line.X2 = end.X;
                        line.Y2 = end.Y;
                    }
                    break;

                case DrawingMode.Arrow:
                    if (shape is System.Windows.Shapes.Polygon arrow)
                    {
                        UpdateArrowShape(arrow, start, end);
                    }
                    break;

                case DrawingMode.Triangle:
                    if (shape is System.Windows.Shapes.Polygon triangle)
                    {
                        UpdateTriangleShape(triangle, start, end);
                    }
                    break;
            }
        }

        private System.Windows.Shapes.Polygon CreateArrowShape(Point start, Point end, Color color, double thickness)
        {
            var arrow = new System.Windows.Shapes.Polygon
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                Fill = new SolidColorBrush(color)
            };
            UpdateArrowShape(arrow, start, end);
            return arrow;
        }

        private void UpdateArrowShape(System.Windows.Shapes.Polygon arrow, Point start, Point end)
        {
            // Calculate arrow line and head
            var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            var arrowHeadLength = 15;
            var arrowHeadWidth = 10;

            // Arrow line endpoints
            var lineEnd = new Point(
                end.X - arrowHeadLength * Math.Cos(angle),
                end.Y - arrowHeadLength * Math.Sin(angle)
            );

            // Arrow head points
            var point1 = end;
            var point2 = new Point(
                end.X - arrowHeadLength * Math.Cos(angle - Math.PI / 6),
                end.Y - arrowHeadLength * Math.Sin(angle - Math.PI / 6)
            );
            var point3 = new Point(
                end.X - arrowHeadLength * Math.Cos(angle + Math.PI / 6),
                end.Y - arrowHeadLength * Math.Sin(angle + Math.PI / 6)
            );

            arrow.Points.Clear();
            arrow.Points.Add(point1);
            arrow.Points.Add(point2);
            arrow.Points.Add(point3);

            // Add line as a separate child if needed - for now just the arrowhead
            // We'll use a Line for the shaft
            if (arrow.Tag == null)
            {
                var line = new System.Windows.Shapes.Line
                {
                    Stroke = arrow.Stroke,
                    StrokeThickness = arrow.StrokeThickness,
                    X1 = start.X,
                    Y1 = start.Y,
                    X2 = lineEnd.X,
                    Y2 = lineEnd.Y
                };
                arrow.Tag = line;
                WhiteboardCanvas.Children.Add(line);
            }
            else if (arrow.Tag is System.Windows.Shapes.Line existingLine)
            {
                existingLine.X1 = start.X;
                existingLine.Y1 = start.Y;
                existingLine.X2 = lineEnd.X;
                existingLine.Y2 = lineEnd.Y;
            }
        }

        private System.Windows.Shapes.Polygon CreateTriangleShape(Point start, Point end, Color color, double thickness)
        {
            var triangle = new System.Windows.Shapes.Polygon
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                Fill = Brushes.Transparent
            };
            UpdateTriangleShape(triangle, start, end);
            return triangle;
        }

        private void UpdateTriangleShape(System.Windows.Shapes.Polygon triangle, Point start, Point end)
        {
            var midX = (start.X + end.X) / 2;
            
            triangle.Points.Clear();
            triangle.Points.Add(new Point(midX, start.Y));           // Top point
            triangle.Points.Add(new Point(start.X, end.Y));          // Bottom-left
            triangle.Points.Add(new Point(end.X, end.Y));            // Bottom-right
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

                    if (ext == ".pdf")
                    {
                        LoadPdfDocument(openFileDialog.FileName);
                    }
                    else if (ext == ".docx" || ext == ".doc")
                    {
                        LoadWordDocument(openFileDialog.FileName);
                    }
                    else
                    {
                        MessageBox.Show("Unsupported file format. Please use PDF or Word documents.",
                            "Unsupported Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening document:\n\n{ex.Message}",
                        "Document Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

                        // Docnet returns BGRA raw pixels — convert to BitmapImage
                        var bitmapSource = BitmapSource.Create(
                            width, height, 96, 96,
                            PixelFormats.Bgra32, null,
                            rawBytes, width * 4);

                        // Convert BitmapSource to BitmapImage for storage
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

            if (_documentPages.Count > 0)
            {
                _currentPageIndex = 0;
                ShowDocumentPage(_currentPageIndex);
                ShowDocNavBar();
            }
            else
            {
                MessageBox.Show("Could not load any pages from this PDF.",
                    "Empty Document", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadWordDocument(string filePath)
        {
            // For Word documents, convert to a simple text-and-image representation
            // We'll render via a temporary HTML approach using WebView2
            try
            {
                _documentPages.Clear();

                // Read the Word document content and display a placeholder message
                // For .docx files, extract text content and render as an image
                var text = ExtractWordText(filePath);
                
                // Create a rendered text image
                var pageImage = RenderTextToImage(text, _currentDocName);
                _documentPages.Add(pageImage);

                _currentPageIndex = 0;
                ShowDocumentPage(_currentPageIndex);
                ShowDocNavBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Word document:\n\n{ex.Message}",
                    "Document Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExtractWordText(string filePath)
        {
            // Simple text extraction from .docx using ZIP + XML parsing
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
                            // Strip XML tags to get plain text
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
            // Render text content as a WPF visual and convert to BitmapImage
            var visual = new DrawingVisual();
            var width = 1600;
            var height = 900;

            using (var context = visual.RenderOpen())
            {
                // White background
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                // Title
                var titleText = new FormattedText(
                    $"📄 {title}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    24, Brushes.Black, 96);
                titleText.MaxTextWidth = width - 100;
                context.DrawText(titleText, new Point(50, 30));

                // Separator
                context.DrawLine(new Pen(Brushes.LightGray, 1), new Point(50, 70), new Point(width - 50, 70));

                // Content
                var contentText = new FormattedText(
                    text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    16, Brushes.Black, 96);
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
                encoder.Save(ms);
                ms.Position = 0;

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

            // Remove previous document image from canvas
            if (_docImage != null && WhiteboardCanvas.Children.Contains(_docImage))
            {
                WhiteboardCanvas.Children.Remove(_docImage);
            }

            // Create a new Image element for this page
            var pageSource = _documentPages[pageIndex];
            _docImage = new Image
            {
                Source = pageSource,
                Stretch = Stretch.Uniform,
                Width = Math.Min(600, WhiteboardCanvas.ActualWidth * 0.6),
                Cursor = Cursors.SizeAll,
                Tag = "DocPage" // Tag to identify document images
            };

            // Calculate height to maintain aspect ratio
            var aspectRatio = (double)pageSource.PixelHeight / pageSource.PixelWidth;
            _docImage.Height = _docImage.Width * aspectRatio;

            // Enable scroll-wheel resizing on the document image
            _docImage.MouseWheel += DocImage_MouseWheel;

            // Place at center of canvas
            var centerX = Math.Max(0, (WhiteboardCanvas.ActualWidth - _docImage.Width) / 2);
            var centerY = Math.Max(0, (WhiteboardCanvas.ActualHeight - _docImage.Height) / 2);
            InkCanvas.SetLeft(_docImage, centerX);
            InkCanvas.SetTop(_docImage, centerY);

            WhiteboardCanvas.Children.Add(_docImage);

            // Update page indicator
            PageIndicator.Text = $"Page {pageIndex + 1} of {_documentPages.Count}";
            PrevPageBtn.IsEnabled = pageIndex > 0;
            NextPageBtn.IsEnabled = pageIndex < _documentPages.Count - 1;
        }

        private void DocImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Image img)
            {
                // Scale factor: scroll up = bigger, scroll down = smaller
                double scale = e.Delta > 0 ? 1.1 : 0.9;
                double newWidth = img.Width * scale;

                // Clamp between 150px and 90% of canvas width
                newWidth = Math.Max(150, Math.Min(WhiteboardCanvas.ActualWidth * 0.9, newWidth));

                // Maintain aspect ratio
                var pageSource = img.Source as BitmapImage;
                if (pageSource != null)
                {
                    double aspectRatio = (double)pageSource.PixelHeight / pageSource.PixelWidth;
                    img.Width = newWidth;
                    img.Height = newWidth * aspectRatio;
                }

                e.Handled = true;
            }
        }

        private void ShowDocNavBar()
        {
            DocNavBar.Visibility = Visibility.Visible;
            DocNameText.Text = _currentDocName;

            // Switch to Select mode so the document can be dragged immediately
            _currentMode = DrawingMode.Select;
            WhiteboardCanvas.EditingMode = InkCanvasEditingMode.Select;
            UpdateToolButtonStyles(SelectButton);
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPageIndex > 0)
            {
                ShowDocumentPage(_currentPageIndex - 1);
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPageIndex < _documentPages.Count - 1)
            {
                ShowDocumentPage(_currentPageIndex + 1);
            }
        }

        private void CloseDocument_Click(object sender, RoutedEventArgs e)
        {
            // Remove document image from canvas
            if (_docImage != null && WhiteboardCanvas.Children.Contains(_docImage))
            {
                WhiteboardCanvas.Children.Remove(_docImage);
            }
            _docImage = null;
            _documentPages.Clear();
            _currentPageIndex = 0;
            _currentDocName = "";
            DocNavBar.Visibility = Visibility.Collapsed;
        }
    }
}
