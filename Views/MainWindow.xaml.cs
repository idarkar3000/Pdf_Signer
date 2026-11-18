using System.IO;
using System.Windows;
using System.Windows.Input;
using Pdf_Signer.ViewModels;

namespace Pdf_Signer
{
    /// <summary>
    /// Vista del editor. Todo el estado esta en <see cref="MainViewModel"/>; aqui
    /// solo se traducen eventos de raton, teclado y arrastre de ficheros.
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isPanning;
        private Point _panStartPoint;
        private double _panStartX;
        private double _panStartY;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
        }

        private MainViewModel ViewModel => (MainViewModel)DataContext;

        #region Arrastrar y soltar PDF

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = TryGetPdf(e) is not null ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string? path = TryGetPdf(e);
            if (path is not null) ViewModel.LoadDocument(path);
        }

        private static string? TryGetPdf(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return null;

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return null;

            return Path.GetExtension(files[0]).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? files[0] : null;
        }

        #endregion

        #region Rueda: zoom y tamano de la firma

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                ViewModel.ZoomByWheel(e.Delta);
            }
            else if (ViewModel.IsPreviewVisible)
            {
                e.Handled = true;
                ViewModel.ResizeSignatureByWheel(e.Delta);
            }
        }

        private void BtnFitPage_Click(object sender, RoutedEventArgs e) =>
            ViewModel.FitToWindow(PdfScrollViewer.ActualHeight);

        #endregion

        #region Desplazar la vista (boton central o espacio + arrastrar)

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
            if (!_isPanning) return;

            Point currentPoint = e.GetPosition(PdfScrollViewer);
            Vector delta = currentPoint - _panStartPoint;

            PdfScrollViewer.ScrollToHorizontalOffset(_panStartX - delta.X);
            PdfScrollViewer.ScrollToVerticalOffset(_panStartY - delta.Y);
        }

        private void PdfScrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPanning || (e.ChangedButton != MouseButton.Middle && e.ChangedButton != MouseButton.Left)) return;

            _isPanning = false;
            PdfScrollViewer.Cursor = Cursors.Arrow;
            PdfScrollViewer.ReleaseMouseCapture();
        }

        #endregion

        #region Interaccion sobre la pagina

        private void PdfContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Quitar el foco del cuadro de texto activo oculta su barra.
            Keyboard.ClearFocus();

            if (Keyboard.IsKeyDown(Key.Space)) return;

            Point position = e.GetPosition(PdfContainer);

            if (ViewModel.HandleCanvasClick(position.X, position.Y, PdfContainer.ActualWidth, PdfContainer.ActualHeight))
            {
                e.Handled = true;
                PdfContainer.CaptureMouse();
            }
        }

        private void PdfContainer_MouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(PdfContainer);

            if (ViewModel.IsPreviewVisible)
            {
                ViewModel.MoveSignaturePreview(position.X, position.Y, PdfContainer.ActualWidth, PdfContainer.ActualHeight);
                return;
            }

            ViewModel.HandleCanvasDrag(position.X, position.Y, PdfContainer.ActualWidth, PdfContainer.ActualHeight);
        }

        private void PdfContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ViewModel.EndDrag();
            PdfContainer.ReleaseMouseCapture();
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (ViewModel.BeginResize(e.GetPosition(PdfContainer).X)) PdfContainer.CaptureMouse();
        }

        #endregion

        private void TxtCurrentPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ViewModel.GoToPage(TxtCurrentPage.Text);
        }
    }
}
