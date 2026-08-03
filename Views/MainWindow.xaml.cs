using Microsoft.Win32;
using Pdf_Signer.Commands;
using PdfEditor.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PdfEditor
{
    // Struct for storing Undo/Redo states
    public class SignatureState
    {
        public BitmapImage SignatureBitmap { get; set; }
        public Point? RelativePosition { get; set; }
        public int TargetPage { get; set; }
        public double WidthInPdfPoints { get; set; }
    }

    public partial class MainWindow : Window
    {
        private string _currentPdfPath;
        private double _currentZoom = 1.0;
        private int _currentPage = 0;
        private int _totalPages = 0;

        private BitmapImage _currentSignature;
        private bool _isPlacingSignature = false;

        private Point? _signatureRelativePosition = null;
        private int _signatureTargetPage = -1;
        private double _signatureWidthInPdfPoints = 140.0;

        // Undo / Redo history for the PDF
        private readonly Stack<SignatureState> _undoStack = new Stack<SignatureState>();
        private readonly Stack<SignatureState> _redoStack = new Stack<SignatureState>();

        private bool _isDraggingSignature = false;
        private Point _dragStartMousePos;
        private Point _dragStartRelativePos;

        private bool _isResizingSignature = false;
        private Point _resizeStartMousePos;
        private double _resizeStartWidthPoints;

        private bool _isPanning = false;
        private Point _panStartPoint;
        private double _panStartX;
        private double _panStartY;

        // Keyboard commands
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ClearSignCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public MainWindow()
        {
            InitializeComponent();

            OpenCommand = new RelayCommand(_ => BtnOpen_Click(this, new RoutedEventArgs()));
            SaveCommand = new RelayCommand(_ => { if (BtnSave.IsEnabled) BtnSave_Click(this, new RoutedEventArgs()); });
            ClearSignCommand = new RelayCommand(_ => { if (BtnClearSign.IsEnabled) BtnClearSign_Click(this, new RoutedEventArgs()); });
            UndoCommand = new RelayCommand(_ => UndoPdfState());
            RedoCommand = new RelayCommand(_ => RedoPdfState());
        }

        #region PDF Undo / Redo Logic

        private void PushUndoState()
        {
            _undoStack.Push(new SignatureState
            {
                SignatureBitmap = _currentSignature,
                RelativePosition = _signatureRelativePosition,
                TargetPage = _signatureTargetPage,
                WidthInPdfPoints = _signatureWidthInPdfPoints
            });

            _redoStack.Clear();
            UpdateUndoRedoButtons();
        }

        private void UndoPdfState()
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(new SignatureState
                {
                    SignatureBitmap = _currentSignature,
                    RelativePosition = _signatureRelativePosition,
                    TargetPage = _signatureTargetPage,
                    WidthInPdfPoints = _signatureWidthInPdfPoints
                });

                SignatureState previousState = _undoStack.Pop();
                ApplySignatureState(previousState);
                UpdateUndoRedoButtons();
            }
        }

        private void RedoPdfState()
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(new SignatureState
                {
                    SignatureBitmap = _currentSignature,
                    RelativePosition = _signatureRelativePosition,
                    TargetPage = _signatureTargetPage,
                    WidthInPdfPoints = _signatureWidthInPdfPoints
                });

                SignatureState nextState = _redoStack.Pop();
                ApplySignatureState(nextState);
                UpdateUndoRedoButtons();
            }
        }

        private void ApplySignatureState(SignatureState state)
        {
            _currentSignature = state.SignatureBitmap;
            _signatureRelativePosition = state.RelativePosition;
            _signatureTargetPage = state.TargetPage;
            _signatureWidthInPdfPoints = state.WidthInPdfPoints;

            if (BtnClearSign != null) BtnClearSign.IsEnabled = _signatureRelativePosition.HasValue;

            UpdatePlacedSignatureVisibility();
        }

        private void UpdateUndoRedoButtons()
        {
            BtnUndoPdf.IsEnabled = _undoStack.Count > 0;
            BtnRedoPdf.IsEnabled = _redoStack.Count > 0;
        }

        private void BtnUndoPdf_Click(object sender, RoutedEventArgs e) => UndoPdfState();
        private void BtnRedoPdf_Click(object sender, RoutedEventArgs e) => RedoPdfState();

        #endregion

        #region PDF File Drag & Drop

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && Path.GetExtension(files[0]).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && Path.GetExtension(files[0]).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    OpenPdfFile(files[0]);
                }
            }
        }

        private void OpenPdfFile(string filePath)
        {
            _currentPdfPath = filePath;
            _currentPage = 0;
            _currentZoom = 1.0;

            _undoStack.Clear();
            _redoStack.Clear();
            UpdateUndoRedoButtons();

            ResetSignaturesUI();
            LoadPdfMetadata();
            RenderCurrentPage();
        }

        #endregion

        #region PDF Loading and Rendering

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Select a PDF File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                OpenPdfFile(openFileDialog.FileName);
            }
        }

        private void LoadPdfMetadata()
        {
            if (string.IsNullOrEmpty(_currentPdfPath) || !File.Exists(_currentPdfPath)) return;

            try
            {
                byte[] pdfBytes = File.ReadAllBytes(_currentPdfPath);
                using (MemoryStream pdfStream = new MemoryStream(pdfBytes))
                using (var document = PdfiumViewer.PdfDocument.Load(pdfStream))
                {
                    _totalPages = document.PageCount;
                    TxtTotalPages.Text = $"/ {_totalPages}";
                    TxtCurrentPage.Text = (_currentPage + 1).ToString();

                    bool hasPages = _totalPages > 0;
                    TxtCurrentPage.IsEnabled = hasPages;
                    BtnZoomIn.IsEnabled = hasPages;
                    BtnZoomOut.IsEnabled = hasPages;
                    BtnZoom100.IsEnabled = hasPages;
                    BtnFitPage.IsEnabled = hasPages;
                    BtnSign.IsEnabled = hasPages;
                    BtnAddText.IsEnabled = hasPages;
                    BtnSave.IsEnabled = hasPages;

                    UpdateNavigationButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading PDF info: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderCurrentPage()
        {
            if (string.IsNullOrEmpty(_currentPdfPath) || !File.Exists(_currentPdfPath)) return;

            try
            {
                byte[] pdfBytes = File.ReadAllBytes(_currentPdfPath);

                using (MemoryStream pdfStream = new MemoryStream(pdfBytes))
                using (var document = PdfiumViewer.PdfDocument.Load(pdfStream))
                {
                    if (document.PageCount > _currentPage)
                    {
                        using (var image = document.Render(_currentPage, 72, 72, true))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Position = 0;

                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = ms;
                                bitmap.EndInit();

                                PdfViewer.Source = bitmap;
                                PdfContainer.Width = bitmap.Width;
                                PdfContainer.Height = bitmap.Height;

                                TxtPlaceholder.Visibility = Visibility.Collapsed;

                                UpdateZoomTransform();
                                UpdatePlacedSignatureVisibility();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error rendering PDF page: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Page Navigation

        private void UpdateNavigationButtons()
        {
            BtnPrevPage.IsEnabled = _currentPage > 0;
            BtnNextPage.IsEnabled = _currentPage < _totalPages - 1;
            TxtCurrentPage.Text = (_currentPage + 1).ToString();
        }

        private void ChangePage(int newPageIndex)
        {
            if (newPageIndex < 0 || newPageIndex >= _totalPages || newPageIndex == _currentPage)
                return;

            _currentPage = newPageIndex;
            UpdateNavigationButtons();
            RenderCurrentPage();
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e) => ChangePage(_currentPage - 1);
        private void BtnNextPage_Click(object sender, RoutedEventArgs e) => ChangePage(_currentPage + 1);

        private void TxtCurrentPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(TxtCurrentPage.Text, out int inputPage))
                {
                    int targetIndex = inputPage - 1;
                    if (targetIndex >= 0 && targetIndex < _totalPages) ChangePage(targetIndex);
                    else TxtCurrentPage.Text = (_currentPage + 1).ToString();
                }
                else TxtCurrentPage.Text = (_currentPage + 1).ToString();
            }
        }

        #endregion

        #region Zoom and Panning

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (e.Delta > 0) _currentZoom += 0.15;
                else if (e.Delta < 0) _currentZoom -= 0.15;

                _currentZoom = Math.Clamp(_currentZoom, 0.2, 4.0);
                UpdateZoomTransform();
            }
            else if (_isPlacingSignature)
            {
                e.Handled = true;
                if (e.Delta > 0) _signatureWidthInPdfPoints += 10.0;
                else if (e.Delta < 0) _signatureWidthInPdfPoints -= 10.0;

                _signatureWidthInPdfPoints = Math.Clamp(_signatureWidthInPdfPoints, 30.0, 600.0);
                UpdateSignaturePreviewSize(e.GetPosition(PdfContainer));
            }
        }

        private void UpdateZoomTransform()
        {
            double baseScale = 2.0;
            double finalScale = baseScale * _currentZoom;

            PdfScaleTransform.ScaleX = finalScale;
            PdfScaleTransform.ScaleY = finalScale;

            TxtZoomLevel.Text = $"{Math.Round(_currentZoom * 100)}%";
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Clamp(_currentZoom + 0.25, 0.2, 4.0);
            UpdateZoomTransform();
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Clamp(_currentZoom - 0.25, 0.2, 4.0);
            UpdateZoomTransform();
        }

        private void BtnZoom100_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = 1.0;
            UpdateZoomTransform();
        }

        private void BtnFitPage_Click(object sender, RoutedEventArgs e)
        {
            if (PdfViewer.Source is BitmapImage bitmap)
            {
                double availableHeight = PdfScrollViewer.ActualHeight - 60;
                if (availableHeight > 0 && bitmap.Height > 0)
                {
                    _currentZoom = Math.Clamp((availableHeight / (bitmap.Height * 2.0)), 0.2, 4.0);
                    UpdateZoomTransform();
                }
            }
        }

        private void PdfScrollViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle ||
               (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space)))
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(PdfScrollViewer);
                _panStartX = PdfScrollViewer.HorizontalOffset;
                _panStartY = PdfScrollViewer.VerticalOffset;

                PdfScrollViewer.Cursor = Cursors.SizeAll;
                PdfScrollViewer.CaptureMouse();
                e.Handled = true;
            }
        }

        private void PdfScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point currentPoint = e.GetPosition(PdfScrollViewer);
                Vector delta = currentPoint - _panStartPoint;

                PdfScrollViewer.ScrollToHorizontalOffset(_panStartX - delta.X);
                PdfScrollViewer.ScrollToVerticalOffset(_panStartY - delta.Y);
            }
        }

        private void PdfScrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning && (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Left))
            {
                _isPanning = false;
                PdfScrollViewer.Cursor = Cursors.Arrow;
                PdfScrollViewer.ReleaseMouseCapture();
            }
        }

        #endregion

        #region Signature UI Management & Stamping

        private void ResetSignaturesUI()
        {
            _isPlacingSignature = false;
            _isDraggingSignature = false;
            _isResizingSignature = false;
            _currentSignature = null;
            _signatureRelativePosition = null;
            _signatureTargetPage = -1;
            Mouse.OverrideCursor = null;

            if (PlacedSignatureContainer != null)
            {
                PlacedSignatureContainer.Visibility = Visibility.Collapsed;
                PlacedSignatureImage.Source = null;
                Canvas.SetLeft(PlacedSignatureContainer, -9999);
                Canvas.SetTop(PlacedSignatureContainer, -9999);
            }

            if (SignaturePreview != null)
            {
                SignaturePreview.Visibility = Visibility.Collapsed;
                SignaturePreview.Source = null;
                Canvas.SetLeft(SignaturePreview, -9999);
                Canvas.SetTop(SignaturePreview, -9999);
            }

            if (BtnClearSign != null) BtnClearSign.IsEnabled = false;
        }

        private void BtnClearSign_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureRelativePosition.HasValue)
            {
                PushUndoState();
                ResetSignaturesUI();
            }
        }

        private void BtnSign_Click(object sender, RoutedEventArgs e)
        {
            SignatureWindow signatureWin = new SignatureWindow();
            signatureWin.Owner = this;

            if (signatureWin.ShowDialog() == true)
            {
                ResetSignaturesUI();

                _currentSignature = signatureWin.SignatureBitmap;
                SignaturePreview.Source = _currentSignature;
                SignaturePreview.Visibility = Visibility.Visible;
                _isPlacingSignature = true;

                Mouse.OverrideCursor = Cursors.Cross;
            }
        }

        private void UpdateSignaturePreviewSize(Point currentMousePos)
        {
            if (_currentSignature == null || PdfContainer.ActualWidth <= 0 || PdfViewer.Source == null) return;

            BitmapImage pdfBitmap = (BitmapImage)PdfViewer.Source;
            double aspectRatio = (double)_currentSignature.PixelHeight / _currentSignature.PixelWidth;

            double scaleFactor = PdfContainer.ActualWidth / pdfBitmap.Width;
            double renderWidth = _signatureWidthInPdfPoints * scaleFactor;
            double renderHeight = renderWidth * aspectRatio;

            SignaturePreview.Width = renderWidth;
            SignaturePreview.Height = renderHeight;

            Canvas.SetLeft(SignaturePreview, currentMousePos.X - (renderWidth / 2.0));
            Canvas.SetTop(SignaturePreview, currentMousePos.Y - (renderHeight / 2.0));
        }

        private void UpdatePlacedSignatureVisibility()
        {
            if (_signatureRelativePosition.HasValue && _signatureTargetPage == _currentPage && _currentSignature != null && PdfViewer.Source != null)
            {
                PlacedSignatureImage.Source = _currentSignature;
                BitmapImage pdfBitmap = (BitmapImage)PdfViewer.Source;

                double aspectRatio = (double)_currentSignature.PixelHeight / _currentSignature.PixelWidth;
                double scaleFactor = PdfContainer.ActualWidth / pdfBitmap.Width;

                double renderWidth = _signatureWidthInPdfPoints * scaleFactor;
                double renderHeight = renderWidth * aspectRatio;

                PlacedSignatureImage.Width = renderWidth;
                PlacedSignatureImage.Height = renderHeight;

                SignatureBorder.Width = renderWidth;
                SignatureBorder.Height = renderHeight;

                PlacedSignatureContainer.Width = renderWidth;
                PlacedSignatureContainer.Height = renderHeight;

                Canvas.SetLeft(ResizeHandle, renderWidth - 5);
                Canvas.SetTop(ResizeHandle, renderHeight - 5);

                double posX = (_signatureRelativePosition.Value.X * PdfContainer.ActualWidth) - (renderWidth / 2.0);
                double posY = (_signatureRelativePosition.Value.Y * PdfContainer.ActualHeight) - (renderHeight / 2.0);

                Canvas.SetLeft(PlacedSignatureContainer, Math.Round(posX));
                Canvas.SetTop(PlacedSignatureContainer, Math.Round(posY));

                PlacedSignatureContainer.Visibility = Visibility.Visible;
            }
            else
            {
                PlacedSignatureContainer.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Canvas Mouse Interaction

        private void PdfContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Quitar el foco de cualquier cuadro de texto activo para ocultar su marco de herramientas
            Keyboard.ClearFocus();

            if (Keyboard.IsKeyDown(Key.Space)) return;

            Point mousePos = e.GetPosition(PdfContainer);

            if (_isPlacingSignature && PdfContainer.ActualWidth > 0 && PdfContainer.ActualHeight > 0)
            {
                e.Handled = true;

                PushUndoState();

                double relativeX = Math.Clamp(mousePos.X / PdfContainer.ActualWidth, 0.0, 1.0);
                double relativeY = Math.Clamp(mousePos.Y / PdfContainer.ActualHeight, 0.0, 1.0);

                _signatureRelativePosition = new Point(relativeX, relativeY);
                _signatureTargetPage = _currentPage;

                _isPlacingSignature = false;
                SignaturePreview.Visibility = Visibility.Collapsed;

                BtnClearSign.IsEnabled = true;
                UpdatePlacedSignatureVisibility();
                Mouse.OverrideCursor = null;
                return;
            }

            if (_signatureRelativePosition.HasValue && _signatureTargetPage == _currentPage && !_isResizingSignature)
            {
                double currentLeft = Canvas.GetLeft(PlacedSignatureContainer);
                double currentTop = Canvas.GetTop(PlacedSignatureContainer);

                Rect signatureBounds = new Rect(currentLeft, currentTop, PlacedSignatureContainer.Width, PlacedSignatureContainer.Height);

                if (signatureBounds.Contains(mousePos))
                {
                    e.Handled = true;
                    PushUndoState();

                    _isDraggingSignature = true;
                    _dragStartMousePos = mousePos;
                    _dragStartRelativePos = _signatureRelativePosition.Value;

                    PdfContainer.CaptureMouse();
                }
            }
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            PushUndoState();

            _isResizingSignature = true;
            _resizeStartMousePos = e.GetPosition(PdfContainer);
            _resizeStartWidthPoints = _signatureWidthInPdfPoints;

            PdfContainer.CaptureMouse();
        }

        private void PdfContainer_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(PdfContainer);

            if (_isPlacingSignature && SignaturePreview.Source != null)
            {
                UpdateSignaturePreviewSize(mousePos);
                return;
            }

            if (_isDraggingSignature && _signatureRelativePosition.HasValue)
            {
                Vector delta = mousePos - _dragStartMousePos;

                double deltaXRel = delta.X / PdfContainer.ActualWidth;
                double deltaYRel = delta.Y / PdfContainer.ActualHeight;

                double newRelX = Math.Clamp(_dragStartRelativePos.X + deltaXRel, 0.0, 1.0);
                double newRelY = Math.Clamp(_dragStartRelativePos.Y + deltaYRel, 0.0, 1.0);

                _signatureRelativePosition = new Point(newRelX, newRelY);
                UpdatePlacedSignatureVisibility();
                return;
            }

            if (_isResizingSignature && PdfViewer.Source != null)
            {
                Vector delta = mousePos - _resizeStartMousePos;
                BitmapImage pdfBitmap = (BitmapImage)PdfViewer.Source;

                double scaleFactor = pdfBitmap.Width / PdfContainer.ActualWidth;
                double deltaPoints = delta.X * scaleFactor;

                _signatureWidthInPdfPoints = Math.Clamp(_resizeStartWidthPoints + deltaPoints, 30.0, 600.0);
                UpdatePlacedSignatureVisibility();
            }
        }

        private void PdfContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingSignature || _isResizingSignature)
            {
                _isDraggingSignature = false;
                _isResizingSignature = false;
                PdfContainer.ReleaseMouseCapture();
            }
        }

        #endregion

        #region Save PDF

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPdfPath)) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Save Signed PDF",
                FileName = Path.GetFileNameWithoutExtension(_currentPdfPath) + "_signed.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    byte[] originalPdfBytes = File.ReadAllBytes(_currentPdfPath);
                    UglyToad.PdfPig.Writer.PdfDocumentBuilder builder = new UglyToad.PdfPig.Writer.PdfDocumentBuilder();

                    using (MemoryStream inputPdfStream = new MemoryStream(originalPdfBytes))
                    using (UglyToad.PdfPig.PdfDocument originalPdf = UglyToad.PdfPig.PdfDocument.Open(inputPdfStream))
                    {
                        for (int pageIndex = 1; pageIndex <= originalPdf.NumberOfPages; pageIndex++)
                        {
                            UglyToad.PdfPig.Writer.PdfPageBuilder pageBuilder = builder.AddPage(originalPdf, pageIndex);
                            var page = originalPdf.GetPage(pageIndex);
                            double pdfWidth = page.Width;
                            double pdfHeight = page.Height;

                            // 1. ESTAMPAR FIRMA
                            if (_signatureRelativePosition.HasValue && pageIndex == _signatureTargetPage + 1 && _currentSignature != null)
                            {
                                Point savedPosition = _signatureRelativePosition.Value;
                                PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                                pngEncoder.Frames.Add(BitmapFrame.Create(_currentSignature));

                                using (MemoryStream ms = new MemoryStream())
                                {
                                    pngEncoder.Save(ms);
                                    byte[] signatureBytes = ms.ToArray();

                                    double sigWidth = _signatureWidthInPdfPoints;
                                    double aspectRatio = (double)pngEncoder.Frames[0].PixelHeight / (double)pngEncoder.Frames[0].PixelWidth;
                                    double sigHeight = sigWidth * aspectRatio;

                                    double clickX = savedPosition.X * pdfWidth;
                                    double clickY = savedPosition.Y * pdfHeight;

                                    double left = Math.Clamp(clickX - (sigWidth / 2.0), 0, pdfWidth - sigWidth);
                                    double top = Math.Clamp(clickY - (sigHeight / 2.0), 0, pdfHeight - sigHeight);

                                    double x1 = left;
                                    double y1 = pdfHeight - top - sigHeight;
                                    double x2 = x1 + sigWidth;
                                    double y2 = y1 + sigHeight;

                                    UglyToad.PdfPig.Core.PdfRectangle placement = new UglyToad.PdfPig.Core.PdfRectangle(x1, y1, x2, y2);

                                    using (MemoryStream stream = new MemoryStream(signatureBytes))
                                    {
                                        pageBuilder.AddPng(stream, placement);
                                    }
                                }
                            }

                            // 2. ESTAMPAR CUADROS DE TEXTO COMO IMÁGENES PNG (Conserva Color y Tamaño Exacto)
                            if (pageIndex == _currentPage + 1)
                            {
                                foreach (var child in PdfContainer.Children)
                                {
                                    if (child is PdfEditor.Views.DraggableTextBlock textBlock)
                                    {
                                        if (string.IsNullOrWhiteSpace(textBlock.Text)) continue;

                                        BitmapImage textBitmap = textBlock.RenderToBitmap();
                                        if (textBitmap == null) continue;

                                        PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                                        pngEncoder.Frames.Add(BitmapFrame.Create(textBitmap));

                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            pngEncoder.Save(ms);
                                            byte[] textBytes = ms.ToArray();

                                            double canvasLeft = Canvas.GetLeft(textBlock);
                                            double canvasTop = Canvas.GetTop(textBlock);

                                            double relativeX = canvasLeft / PdfContainer.ActualWidth;
                                            double relativeY = canvasTop / PdfContainer.ActualHeight;

                                            double textWidthInPdf = (textBitmap.PixelWidth / PdfContainer.ActualWidth) * pdfWidth;
                                            double textHeightInPdf = (textBitmap.PixelHeight / PdfContainer.ActualHeight) * pdfHeight;

                                            double x1 = relativeX * pdfWidth;
                                            double y1 = pdfHeight - (relativeY * pdfHeight) - textHeightInPdf;
                                            double x2 = x1 + textWidthInPdf;
                                            double y2 = y1 + textHeightInPdf;

                                            UglyToad.PdfPig.Core.PdfRectangle placement = new UglyToad.PdfPig.Core.PdfRectangle(x1, y1, x2, y2);

                                            using (MemoryStream stream = new MemoryStream(textBytes))
                                            {
                                                pageBuilder.AddPng(stream, placement);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    File.WriteAllBytes(saveFileDialog.FileName, builder.Build());

                    // 🌟 CLAVE: LIMPIAR CONTROLES FLOTANTES PARA EVITAR DUPLICADOS AL RECARGAR
                    ResetSignaturesUI();

                    // Eliminar los TextBlocks del Canvas
                    var textBlocksToRemove = new List<UIElement>();
                    foreach (UIElement child in PdfContainer.Children)
                    {
                        if (child is PdfEditor.Views.DraggableTextBlock)
                        {
                            textBlocksToRemove.Add(child);
                        }
                    }
                    foreach (var tb in textBlocksToRemove)
                    {
                        PdfContainer.Children.Remove(tb);
                    }

                    // Recargar el documento firmado resultante
                    _currentPdfPath = saveFileDialog.FileName;
                    LoadPdfMetadata();
                    RenderCurrentPage();

                    MessageBox.Show("Signed PDF saved successfully!", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving the PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddText_Click(object sender, RoutedEventArgs e)
        {
            var newTextBlock = new DraggableTextBlock();

            // Posicionar el nuevo cuadro en el centro visible del PdfContainer
            Canvas.SetLeft(newTextBlock, 50);
            Canvas.SetTop(newTextBlock, 50);

            // Añadirlo dentro del contenedor del PDF para que escale con el Zoom
            PdfContainer.Children.Add(newTextBlock);
        }

        #endregion
    }
}