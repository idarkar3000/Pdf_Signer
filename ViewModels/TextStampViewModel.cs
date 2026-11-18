using System;
using System.Windows;
using System.Windows.Media;
using Pdf_Signer.Commands;
using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Placement;
using Pdf_Signer.Services;

namespace Pdf_Signer.ViewModels
{
    /// <summary>
    /// Un cuadro de texto colocado sobre el documento. Guarda el texto y su
    /// aspecto; el PNG se pide a la vista cuando hace falta estampar, para no
    /// renderizar en cada tecla.
    /// </summary>
    public sealed class TextStampViewModel : ViewModelBase
    {
        public const double MinFontSize = 5.0;
        public const double MaxFontSize = 199.0;
        public const double DefaultFontSize = 16.0;

        private string _text = string.Empty;
        private double _fontSize = DefaultFontSize;
        private Color _color = Colors.Red;
        private Brush _brush = Brushes.Red;
        private double _left;
        private double _top;
        private int _targetPage;
        private int _currentPage;

        public TextStampViewModel(Action<TextStampViewModel>? deleteRequested = null)
        {
            DeleteCommand = new RelayCommand(_ => deleteRequested?.Invoke(this));
        }

        public RelayCommand DeleteCommand { get; }

        /// <summary>
        /// Devuelve el texto renderizado tal y como se ve ahora mismo. Lo asigna
        /// la vista: convertir un control en bitmap es cosa suya, no del ViewModel.
        /// </summary>
        public Func<RenderedText?>? PngProvider { get; set; }

        public string Text
        {
            get => _text;
            set
            {
                SetProperty(ref _text, value ?? string.Empty);
            }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                SetProperty(ref _fontSize, Math.Clamp(value, MinFontSize, MaxFontSize));
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (!SetProperty(ref _color, value)) return;

                SolidColorBrush brush = new(value);
                brush.Freeze();
                _brush = brush;

                OnPropertyChanged(nameof(Brush));
            }
        }

        /// <summary>Color listo para enlazar con <c>Foreground</c>.</summary>
        public Brush Brush => _brush;

        /// <summary>Posicion horizontal dentro del lienzo, en pixeles.</summary>
        public double Left
        {
            get => _left;
            set => SetProperty(ref _left, value);
        }

        /// <summary>Posicion vertical dentro del lienzo, en pixeles.</summary>
        public double Top
        {
            get => _top;
            set => SetProperty(ref _top, value);
        }

        /// <summary>Pagina a la que pertenece el cuadro, desde cero.</summary>
        public int TargetPage
        {
            get => _targetPage;
            set
            {
                if (!SetProperty(ref _targetPage, value)) return;
                OnPropertyChanged(nameof(PageVisibility));
            }
        }

        /// <summary>
        /// Visible solo en su pagina. Se usa <c>Hidden</c> y no <c>Collapsed</c>
        /// para que el control siga midiendose y se pueda renderizar aunque no se
        /// este viendo.
        /// </summary>
        public Visibility PageVisibility =>
            _targetPage == _currentPage ? Visibility.Visible : Visibility.Hidden;

        public void SetCurrentPage(int page)
        {
            if (_currentPage == page) return;

            _currentPage = page;
            OnPropertyChanged(nameof(PageVisibility));
        }

        /// <summary>Convierte el cuadro en algo que el nucleo sepa estampar.</summary>
        public TextStamp? ToStamp(int canvasWidth, int canvasHeight, double pageWidth, double pageHeight)
        {
            RenderedText? rendered = PngProvider?.Invoke();
            if (rendered is null) return null;

            return TextStampFactory.Create(
                Text,
                rendered.Png,
                Left + rendered.OffsetX,
                Top + rendered.OffsetY,
                canvasWidth,
                canvasHeight,
                pageWidth,
                pageHeight,
                FontSize,
                Argb,
                TargetPage);
        }

        /// <summary>Reconstruye el cuadro a partir de un sello guardado (al deshacer).</summary>
        public static TextStampViewModel FromStamp(
            TextStamp stamp,
            int canvasWidth,
            int canvasHeight,
            Action<TextStampViewModel>? deleteRequested = null)
        {
            TextStampViewModel viewModel = new(deleteRequested)
            {
                Text = stamp.Text,
                FontSize = stamp.FontSize > 0 ? stamp.FontSize : DefaultFontSize,
                Color = Color.FromArgb(
                    (byte)(stamp.ColorArgb >> 24),
                    (byte)(stamp.ColorArgb >> 16),
                    (byte)(stamp.ColorArgb >> 8),
                    (byte)stamp.ColorArgb),
                Left = stamp.TopLeft.X * canvasWidth,
                Top = stamp.TopLeft.Y * canvasHeight,
                TargetPage = stamp.TargetPage
            };

            return viewModel;
        }

        private int Argb => (_color.A << 24) | (_color.R << 16) | (_color.G << 8) | _color.B;
    }
}
