using Pdf_Signer.Commands;
using Pdf_Signer.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pdf_Signer
{
    public partial class SignatureWindow : Window
    {
        /// <summary>Firma dibujada, ya rasterizada. <c>null</c> mientras no se acepte el dialogo.</summary>
        public BitmapImage? SignatureBitmap { get; private set; }

        private readonly Stack<Stroke> _undoStack = new Stack<Stroke>();
        private readonly Stack<Stroke> _redoStack = new Stack<Stroke>();

        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public SignatureWindow()
        {
            InitializeComponent();

            SignatureCanvas.DefaultDrawingAttributes.Color = Colors.Black;
            SignatureCanvas.DefaultDrawingAttributes.Width = 3;
            SignatureCanvas.DefaultDrawingAttributes.Height = 3;

            UndoCommand = new RelayCommand(_ => UndoStroke());
            RedoCommand = new RelayCommand(_ => RedoStroke());

            UpdateUndoRedoState();
        }

        private void SignatureCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            _undoStack.Push(e.Stroke);
            _redoStack.Clear();
            UpdateUndoRedoState();
        }

        private void UndoStroke()
        {
            if (_undoStack.Count > 0)
            {
                Stroke stroke = _undoStack.Pop();
                SignatureCanvas.Strokes.Remove(stroke);
                _redoStack.Push(stroke);
                UpdateUndoRedoState();
            }
        }

        private void RedoStroke()
        {
            if (_redoStack.Count > 0)
            {
                Stroke stroke = _redoStack.Pop();
                SignatureCanvas.Strokes.Add(stroke);
                _undoStack.Push(stroke);
                UpdateUndoRedoState();
            }
        }

        private void UpdateUndoRedoState()
        {
            BtnUndo.IsEnabled = _undoStack.Count > 0;
            BtnRedo.IsEnabled = _redoStack.Count > 0;
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e) => UndoStroke();
        private void BtnRedo_Click(object sender, RoutedEventArgs e) => RedoStroke();

        private void ModeMode_Changed(object sender, RoutedEventArgs e)
        {
            if (SignatureCanvas == null) return;

            SignatureCanvas.EditingMode = (RbErase.IsChecked == true)
                ? InkCanvasEditingMode.EraseByStroke
                : InkCanvasEditingMode.Ink;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            SignatureCanvas.Strokes.Clear();
            UpdateUndoRedoState();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (SignatureCanvas.Strokes.Count == 0)
            {
                MessageBox.Show("Por favor, dibuja una firma antes de aceptar.", "Firma vacía", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (SignatureRasterizer.Render(SignatureCanvas.Strokes) is not { } raster) return;

                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(raster));

                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    ms.Position = 0;

                    SignatureBitmap = new BitmapImage();
                    SignatureBitmap.BeginInit();
                    SignatureBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    SignatureBitmap.StreamSource = ms;
                    SignatureBitmap.EndInit();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar la firma: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}