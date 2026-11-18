using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Pdf_Signer.Commands;
using Pdf_Signer.Core.History;
using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Pdf;
using Pdf_Signer.Core.Placement;
using Pdf_Signer.Services;

namespace Pdf_Signer.ViewModels
{
    /// <summary>
    /// Todo el estado y el comportamiento del editor. La vista solo traduce
    /// eventos de raton y teclado a llamadas a este objeto.
    /// </summary>
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly IPdfRenderService _renderer;
        private readonly IDialogService _dialogs;
        private readonly UndoRedoHistory<EditorState> _history = new();

        private string? _pdfPath;
        private int _currentPage;
        private int _totalPages;
        private double _zoom = 1.0;
        private BitmapImage? _pageImage;
        private double _pageWidthPoints;
        private double _pageHeightPoints;

        private SignatureStamp? _signature;
        private BitmapImage? _signatureImage;
        private SignatureDraft? _draft;
        private double _signatureWidthPoints = SignaturePlacement.DefaultWidthPoints;
        private RelativePoint _previewCenter = RelativePoint.Center;

        private double _signatureLeft;
        private double _signatureTop;
        private double _signatureWidth;
        private double _signatureHeight;
        private bool _isSignatureVisible;

        private double _previewLeft;
        private double _previewTop;
        private double _previewWidth;
        private double _previewHeight;

        private bool _isDragging;
        private bool _dragMoved;
        private Point _dragStart;
        private RelativePoint _dragStartCenter;

        private bool _isResizing;
        private bool _resizeMoved;
        private double _resizeStartX;
        private double _resizeStartWidth;

        public MainViewModel()
            : this(new PdfiumRenderService(), new DialogService())
        {
        }

        public MainViewModel(IPdfRenderService renderer, IDialogService dialogs)
        {
            ArgumentNullException.ThrowIfNull(renderer);
            ArgumentNullException.ThrowIfNull(dialogs);

            _renderer = renderer;
            _dialogs = dialogs;

            OpenCommand = new RelayCommand(_ => Open());
            SaveCommand = new RelayCommand(_ => Save(), _ => HasDocument);
            SignCommand = new RelayCommand(_ => DrawSignature(), _ => HasDocument);
            ClearSignatureCommand = new RelayCommand(_ => ClearSignature(), _ => _signature is not null);
            AddTextCommand = new RelayCommand(_ => AddText(), _ => HasDocument);
            UndoCommand = new RelayCommand(_ => Undo(), _ => _history.CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => _history.CanRedo);
            PreviousPageCommand = new RelayCommand(_ => GoToPage(_currentPage - 1), _ => _currentPage > 0);
            NextPageCommand = new RelayCommand(_ => GoToPage(_currentPage + 1), _ => _currentPage < _totalPages - 1);
            ZoomInCommand = new RelayCommand(_ => SetZoom(ZoomMath.ZoomIn(_zoom)), _ => HasDocument);
            ZoomOutCommand = new RelayCommand(_ => SetZoom(ZoomMath.ZoomOut(_zoom)), _ => HasDocument);
            ResetZoomCommand = new RelayCommand(_ => SetZoom(1.0), _ => HasDocument);
        }

        #region Comandos

        public RelayCommand OpenCommand { get; }

        public RelayCommand SaveCommand { get; }

        public RelayCommand SignCommand { get; }

        public RelayCommand ClearSignatureCommand { get; }

        public RelayCommand AddTextCommand { get; }

        public RelayCommand UndoCommand { get; }

        public RelayCommand RedoCommand { get; }

        public RelayCommand PreviousPageCommand { get; }

        public RelayCommand NextPageCommand { get; }

        public RelayCommand ZoomInCommand { get; }

        public RelayCommand ZoomOutCommand { get; }

        public RelayCommand ResetZoomCommand { get; }

        #endregion

        #region Estado del documento

        public BitmapImage? PageImage => _pageImage;

        /// <summary>Ancho del lienzo en pixeles. <c>NaN</c> cuando no hay documento, para que el canvas se ajuste al marcador de posicion.</summary>
        public double PagePixelWidth => _pageImage?.PixelWidth ?? double.NaN;

        public double PagePixelHeight => _pageImage?.PixelHeight ?? double.NaN;

        public bool HasDocument => _pageImage is not null;

        public bool IsPlaceholderVisible => !HasDocument;

        public string CurrentPageNumber => (_currentPage + 1).ToString();

        public string TotalPagesText => $"/ {_totalPages}";

        public double Zoom => _zoom;

        /// <summary>Escala real que se aplica al lienzo.</summary>
        public double ZoomScale => ZoomMath.EffectiveScale(_zoom);

        public string ZoomText => ZoomMath.FormatPercent(_zoom);

        public ObservableCollection<TextStampViewModel> TextStamps { get; } = new();

        #endregion

        #region Sobreimpresiones

        public BitmapImage? SignatureImage => _signatureImage;

        public bool IsSignatureVisible => _isSignatureVisible;

        public double SignatureLeft => _signatureLeft;

        public double SignatureTop => _signatureTop;

        public double SignatureWidth => _signatureWidth;

        public double SignatureHeight => _signatureHeight;

        public bool IsPreviewVisible => _draft is not null;

        public BitmapImage? PreviewImage => _draft?.Image;

        public double PreviewLeft => _previewLeft;

        public double PreviewTop => _previewTop;

        public double PreviewWidth => _previewWidth;

        public double PreviewHeight => _previewHeight;

        /// <summary>Cursor del lienzo: cruz mientras se esta colocando una firma.</summary>
        public Cursor? CanvasCursor => _draft is not null ? Cursors.Cross : null;

        #endregion

        #region Abrir, guardar y cerrar

        private void Open()
        {
            string? path = _dialogs.PickPdfToOpen();
            if (path is null) return;

            LoadDocument(path, resetView: true);
        }

        /// <summary>Carga un PDF (tambien se usa al soltar un fichero sobre la ventana).</summary>
        public void LoadDocument(string path, bool resetView = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            _pdfPath = path;

            if (resetView)
            {
                _currentPage = 0;
                SetZoom(1.0);
            }

            _history.Clear();
            _signature = null;
            _signatureImage = null;
            _draft = null;
            TextStamps.Clear();

            try
            {
                _totalPages = _renderer.GetPageCount(path);
            }
            catch (Exception ex)
            {
                _totalPages = 0;
                _dialogs.ShowError($"Error loading PDF info: {ex.Message}");
            }

            OnPropertyChanged(nameof(CurrentPageNumber));
            OnPropertyChanged(nameof(TotalPagesText));
            OnPropertyChanged(nameof(IsPreviewVisible));
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(CanvasCursor));

            RenderCurrentPage();
            RefreshCommands();
        }

        private void Save()
        {
            if (_pdfPath is null) return;

            string suggestedName = Path.GetFileNameWithoutExtension(_pdfPath) + "_signed.pdf";
            string? target = _dialogs.PickPdfToSave(suggestedName);
            if (target is null) return;

            if (SaveTo(target)) _dialogs.ShowInfo("Signed PDF saved successfully!");
        }

        /// <summary>
        /// Estampa todo lo colocado y escribe el resultado en
        /// <paramref name="targetPath"/>. Sin dialogos, para poder probarlo.
        /// Devuelve <c>false</c> si no habia documento o si algo fallo.
        /// </summary>
        public bool SaveTo(string targetPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

            if (_pdfPath is null) return false;

            try
            {
                byte[] stamped = PdfStamper.Stamp(File.ReadAllBytes(_pdfPath), CaptureState());
                File.WriteAllBytes(targetPath, stamped);

                // El PDF guardado ya lleva todo dentro: se recarga el resultado y
                // el historial se vacia, que si no un Ctrl+Z volveria a estampar
                // lo mismo sobre el fichero nuevo.
                LoadDocument(targetPath, resetView: false);
                return true;
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Error saving the PDF: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Navegacion y zoom

        private void GoToPage(int page)
        {
            if (page < 0 || page >= _totalPages || page == _currentPage) return;

            _currentPage = page;
            OnPropertyChanged(nameof(CurrentPageNumber));

            RenderCurrentPage();
            UpdateTextStampPages();
            RefreshCommands();
        }

        /// <summary>Salta a la pagina escrita en la caja de texto (1-based).</summary>
        public void GoToPage(string? pageText)
        {
            if (int.TryParse(pageText, out int page)) GoToPage(page - 1);

            // Si el numero no vale, la caja vuelve a mostrar la pagina actual.
            OnPropertyChanged(nameof(CurrentPageNumber));
        }

        private void SetZoom(double zoom)
        {
            double clamped = ZoomMath.Clamp(zoom);
            if (Math.Abs(clamped - _zoom) < 0.0001) return;

            _zoom = clamped;
            OnPropertyChanged(nameof(Zoom));
            OnPropertyChanged(nameof(ZoomScale));
            OnPropertyChanged(nameof(ZoomText));
        }

        /// <summary>Zoom con Ctrl + rueda.</summary>
        public void ZoomByWheel(int wheelDelta) => SetZoom(ZoomMath.Wheel(_zoom, wheelDelta));

        /// <summary>Ajusta la pagina al alto visible del visor.</summary>
        public void FitToWindow(double availableHeight)
        {
            if (_pageImage is null) return;

            SetZoom(ZoomMath.FitPage(availableHeight, _pageImage.PixelHeight));
        }

        private void RenderCurrentPage()
        {
            if (_pdfPath is null || _totalPages <= 0)
            {
                SetPageImage(null);
                return;
            }

            try
            {
                RenderedPage page = _renderer.RenderPage(_pdfPath, _currentPage);
                _pageWidthPoints = page.WidthPoints;
                _pageHeightPoints = page.HeightPoints;
                SetPageImage(page.Image);
            }
            catch (Exception ex)
            {
                SetPageImage(null);
                _dialogs.ShowError($"Error rendering PDF page: {ex.Message}");
            }
        }

        private void SetPageImage(BitmapImage? image)
        {
            _pageImage = image;

            OnPropertyChanged(nameof(PageImage));
            OnPropertyChanged(nameof(PagePixelWidth));
            OnPropertyChanged(nameof(PagePixelHeight));
            OnPropertyChanged(nameof(HasDocument));
            OnPropertyChanged(nameof(IsPlaceholderVisible));

            UpdateSignatureOverlay();
            RefreshCommands();
        }

        #endregion

        #region Firma

        private void DrawSignature()
        {
            if (!HasDocument) return;

            BitmapImage? image = _dialogs.DrawSignature();
            if (image is null) return;

            _draft = new SignatureDraft(image, BitmapHelpers.ToPng(image));
            _signatureWidthPoints = SignaturePlacement.DefaultWidthPoints;
            _previewCenter = RelativePoint.Center;

            OnPropertyChanged(nameof(IsPreviewVisible));
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(CanvasCursor));
            UpdatePreview();
            RefreshCommands();
        }

        private void ClearSignature()
        {
            if (_signature is null) return;

            PushUndoState();
            _signature = null;
            _signatureImage = null;
            UpdateSignatureOverlay();
            RefreshCommands();
        }

        /// <summary>
        /// Clic sobre la pagina. Devuelve <c>true</c> si el clic lo ha consumido
        /// el editor (colocar la firma o empezar a arrastrarla).
        /// </summary>
        public bool HandleCanvasClick(double x, double y, double canvasWidth, double canvasHeight)
        {
            if (_pageImage is null || canvasWidth <= 0 || canvasHeight <= 0) return false;

            if (_draft is not null)
            {
                PlaceSignature(x, y, canvasWidth, canvasHeight);
                return true;
            }

            if (_signature is not null && !_isResizing && IsOverSignature(x, y))
            {
                // El estado se guarda al primer movimiento, no aqui: un clic sin
                // arrastre no debe ensuciar el historial.
                _isDragging = true;
                _dragMoved = false;
                _dragStart = new Point(x, y);
                _dragStartCenter = _signature.Center;
                return true;
            }

            return false;
        }

        private bool IsOverSignature(double x, double y) =>
            _isSignatureVisible &&
            x >= _signatureLeft && x <= _signatureLeft + _signatureWidth &&
            y >= _signatureTop && y <= _signatureTop + _signatureHeight;

        private void PlaceSignature(double x, double y, double canvasWidth, double canvasHeight)
        {
            if (_draft is null) return;

            PushUndoState();

            _signature = new SignatureStamp
            {
                PngBytes = _draft.Png,
                Center = RelativePlacement.FromCanvas(x, y, canvasWidth, canvasHeight),
                TargetPage = _currentPage,
                WidthPoints = _signatureWidthPoints
            };

            _signatureImage = _draft.Image;
            _draft = null;

            OnPropertyChanged(nameof(IsPreviewVisible));
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(CanvasCursor));
            UpdateSignatureOverlay();
            RefreshCommands();
        }

        /// <summary>Arrastra la firma colocada. Devuelve <c>true</c> si algo ha cambiado.</summary>
        public bool HandleCanvasDrag(double x, double y, double canvasWidth, double canvasHeight)
        {
            if (_isResizing) return HandleResize(x, canvasWidth);

            if (!_isDragging || _signature is null || _pageImage is null) return false;

            double deltaX = x - _dragStart.X;
            double deltaY = y - _dragStart.Y;

            // Un par de pixeles de temblor no son un arrastre.
            if (!_dragMoved && Math.Abs(deltaX) < 2 && Math.Abs(deltaY) < 2) return false;

            if (!_dragMoved)
            {
                _dragMoved = true;
                PushUndoState();
            }

            RelativePoint center = RelativePlacement.MoveBy(_dragStartCenter, deltaX, deltaY, canvasWidth, canvasHeight);
            _signature = _signature with { Center = center };

            UpdateSignatureOverlay();
            return true;
        }

        /// <summary>Empieza a redimensionar con el tirador de la esquina.</summary>
        public bool BeginResize(double x)
        {
            if (_signature is null) return false;

            _isResizing = true;
            _resizeMoved = false;
            _resizeStartX = x;
            _resizeStartWidth = _signature.WidthPoints;
            return true;
        }

        private bool HandleResize(double x, double canvasWidth)
        {
            if (!_isResizing || _signature is null || _pageImage is null) return false;

            double delta = x - _resizeStartX;
            if (!_resizeMoved && Math.Abs(delta) < 2) return false;

            if (!_resizeMoved)
            {
                _resizeMoved = true;
                PushUndoState();
            }

            double pixelsPerPoint = _pageImage.PixelWidth / _pageWidthPoints;
            double width = SignaturePlacement.ClampWidth(_resizeStartWidth + (delta / pixelsPerPoint));
            _signature = _signature with { WidthPoints = width };

            UpdateSignatureOverlay();
            return true;
        }

        /// <summary>Fin del arrastre o del redimensionado.</summary>
        public void EndDrag()
        {
            _isDragging = false;
            _isResizing = false;
        }

        /// <summary>Mueve la previsualizacion de la firma que sigue al raton.</summary>
        public void MoveSignaturePreview(double x, double y, double canvasWidth, double canvasHeight)
        {
            if (_draft is null) return;

            _previewCenter = RelativePlacement.FromCanvas(x, y, canvasWidth, canvasHeight);
            UpdatePreview();
        }

        /// <summary>Cambia el tamano de la firma con la rueda mientras se coloca.</summary>
        public void ResizeSignatureByWheel(int wheelDelta)
        {
            if (_draft is null) return;

            double step = wheelDelta > 0 ? 10.0 : -10.0;
            _signatureWidthPoints = SignaturePlacement.ClampWidth(_signatureWidthPoints + step);
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_draft is null || _pageImage is null || _pageWidthPoints <= 0) return;

            double aspectRatio = (double)_draft.Image.PixelHeight / _draft.Image.PixelWidth;
            PlacementRect rect = SignaturePlacement.Compute(
                _previewCenter,
                _signatureWidthPoints,
                aspectRatio,
                _pageWidthPoints,
                _pageHeightPoints);

            double pixelsPerPoint = _pageImage.PixelWidth / _pageWidthPoints;
            _previewLeft = rect.Left * pixelsPerPoint;
            _previewTop = rect.Top * pixelsPerPoint;
            _previewWidth = rect.Width * pixelsPerPoint;
            _previewHeight = rect.Height * pixelsPerPoint;

            OnPropertyChanged(nameof(PreviewLeft));
            OnPropertyChanged(nameof(PreviewTop));
            OnPropertyChanged(nameof(PreviewWidth));
            OnPropertyChanged(nameof(PreviewHeight));
        }

        private void UpdateSignatureOverlay()
        {
            bool visible = false;

            if (_signature is { } signature && _signatureImage is not null && _pageImage is not null && _pageWidthPoints > 0)
            {
                visible = signature.TargetPage == _currentPage;

                double aspectRatio = (double)_signatureImage.PixelHeight / _signatureImage.PixelWidth;
                PlacementRect rect = SignaturePlacement.Compute(
                    signature.Center,
                    signature.WidthPoints,
                    aspectRatio,
                    _pageWidthPoints,
                    _pageHeightPoints);

                double pixelsPerPoint = _pageImage.PixelWidth / _pageWidthPoints;
                _signatureLeft = Math.Round(rect.Left * pixelsPerPoint);
                _signatureTop = Math.Round(rect.Top * pixelsPerPoint);
                _signatureWidth = rect.Width * pixelsPerPoint;
                _signatureHeight = rect.Height * pixelsPerPoint;
            }

            _isSignatureVisible = visible;

            OnPropertyChanged(nameof(IsSignatureVisible));
            OnPropertyChanged(nameof(SignatureLeft));
            OnPropertyChanged(nameof(SignatureTop));
            OnPropertyChanged(nameof(SignatureWidth));
            OnPropertyChanged(nameof(SignatureHeight));
            OnPropertyChanged(nameof(SignatureImage));
        }

        #endregion

        #region Cuadros de texto

        private void AddText()
        {
            if (!HasDocument) return;

            PushUndoState();

            TextStampViewModel stamp = new(RemoveTextStamp)
            {
                Left = 50,
                Top = 50,
                TargetPage = _currentPage
            };
            stamp.SetCurrentPage(_currentPage);

            TextStamps.Add(stamp);
            RefreshCommands();
        }

        private void RemoveTextStamp(TextStampViewModel stamp)
        {
            if (!TextStamps.Contains(stamp)) return;

            PushUndoState();
            TextStamps.Remove(stamp);
            RefreshCommands();
        }

        /// <summary>
        /// Guarda el estado justo antes de mover un cuadro de texto. Lo llama la
        /// vista en el primer movimiento, no al pulsar: un clic sin arrastre no
        /// debe ensuciar el historial.
        /// </summary>
        public void BeginTextStampMove() => PushUndoState();

        private void UpdateTextStampPages()
        {
            foreach (TextStampViewModel stamp in TextStamps) stamp.SetCurrentPage(_currentPage);
        }

        #endregion

        #region Historial

        private EditorState CaptureState()
        {
            List<TextStamp> stamps = new();

            if (_pageImage is not null)
            {
                foreach (TextStampViewModel stamp in TextStamps)
                {
                    TextStamp? captured = stamp.ToStamp(
                        _pageImage.PixelWidth,
                        _pageImage.PixelHeight,
                        _pageWidthPoints,
                        _pageHeightPoints);

                    if (captured is not null) stamps.Add(captured);
                }
            }

            return new EditorState { Signature = _signature, TextStamps = stamps };
        }

        private void PushUndoState()
        {
            _history.Push(CaptureState());
            RefreshCommands();
        }

        private void Undo()
        {
            EditorState? previous = _history.Undo(CaptureState());
            if (previous is null) return;

            ApplyState(previous);
        }

        private void Redo()
        {
            EditorState? next = _history.Redo(CaptureState());
            if (next is null) return;

            ApplyState(next);
        }

        private void ApplyState(EditorState state)
        {
            _signature = state.Signature;
            _signatureImage = state.Signature is null ? null : BitmapHelpers.FromPng(state.Signature.PngBytes);

            TextStamps.Clear();

            if (_pageImage is not null)
            {
                foreach (TextStamp stamp in state.TextStamps)
                {
                    TextStampViewModel viewModel = TextStampViewModel.FromStamp(
                        stamp,
                        _pageImage.PixelWidth,
                        _pageImage.PixelHeight,
                        RemoveTextStamp);

                    viewModel.SetCurrentPage(_currentPage);
                    TextStamps.Add(viewModel);
                }
            }

            UpdateSignatureOverlay();
            RefreshCommands();
        }

        #endregion

        private void RefreshCommands()
        {
            SaveCommand.RaiseCanExecuteChanged();
            SignCommand.RaiseCanExecuteChanged();
            ClearSignatureCommand.RaiseCanExecuteChanged();
            AddTextCommand.RaiseCanExecuteChanged();
            UndoCommand.RaiseCanExecuteChanged();
            RedoCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            ZoomInCommand.RaiseCanExecuteChanged();
            ZoomOutCommand.RaiseCanExecuteChanged();
            ResetZoomCommand.RaiseCanExecuteChanged();
        }

        private sealed record SignatureDraft(BitmapImage Image, byte[] Png);
    }
}
