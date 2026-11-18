using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Pdf_Signer.Services;
using Pdf_Signer.ViewModels;

namespace Pdf_Signer.Views
{
    /// <summary>
    /// Cuadro de texto que se puede arrastrar sobre la pagina. Solo se ocupa de
    /// lo visual: los datos viven en <see cref="TextStampViewModel"/>.
    /// </summary>
    public partial class DraggableTextBlock : UserControl
    {
        private bool _isDragging;
        private bool _moved;
        private Point _grabOffset;
        private Canvas? _pageCanvas;

        public DraggableTextBlock()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private TextStampViewModel? Stamp => DataContext as TextStampViewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Stamp is not { } stamp) return;

            stamp.PngProvider = RenderPng;
            TxtFontSize.Text = stamp.FontSize.ToString("0");
            _pageCanvas = FindPageCanvas();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (Stamp is { } stamp) stamp.PngProvider = null;
        }

        #region Aspecto

        private void UserControl_GotFocus(object sender, RoutedEventArgs e) => ShowControls(true);

        private void TxtInput_GotFocus(object sender, RoutedEventArgs e) => ShowControls(true);

        private void UserControl_LostFocus(object sender, RoutedEventArgs e) => ShowControls(false);

        private void ShowControls(bool visible)
        {
            if (visible)
            {
                MainBorder.BorderThickness = new Thickness(1.5);
                MainBorder.Background = new SolidColorBrush(Color.FromArgb(230, 37, 37, 38));
                ControlPanel.Visibility = Visibility.Visible;
            }
            else
            {
                MainBorder.BorderThickness = new Thickness(0);
                MainBorder.Background = Brushes.Transparent;
                ControlPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtPlaceholder.Visibility = string.IsNullOrEmpty(TxtInput.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtInput == null || Stamp is not { } stamp) return;

            if (double.TryParse(TxtFontSize.Text, out double size) && size > TextStampViewModel.MinFontSize)
            {
                stamp.FontSize = size;
            }
        }

        private void BtnPickColor_Click(object sender, RoutedEventArgs e)
        {
            if (Stamp is not { } stamp) return;

            ContextMenu menu = new();

            (string Name, Color Color)[] colors =
            [
                ("Red", Colors.Red),
                ("Black", Colors.Black),
                ("Blue", Colors.DeepSkyBlue),
                ("Green", Colors.LimeGreen),
                ("Orange", Colors.Orange),
                ("Purple", Colors.MediumPurple),
                ("White", Colors.White)
            ];

            foreach ((string name, Color color) in colors)
            {
                MenuItem item = new()
                {
                    Header = name,
                    Icon = new Border
                    {
                        Width = 12,
                        Height = 12,
                        Background = new SolidColorBrush(color),
                        CornerRadius = new CornerRadius(2)
                    }
                };

                item.Click += (_, _) => stamp.Color = color;
                menu.Items.Add(item);
            }

            menu.PlacementTarget = BtnPickColor;
            menu.IsOpen = true;
        }

        #endregion

        #region Arrastre

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            ShowControls(true);

            // Los clics dentro de la barra de herramientas no arrastran el cuadro.
            if (e.OriginalSource is TextBox or Button or Ellipse or MenuItem) return;

            _pageCanvas ??= FindPageCanvas();
            if (_pageCanvas is null) return;

            _isDragging = true;
            _moved = false;
            _grabOffset = e.GetPosition(this);
            CaptureMouse();
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _pageCanvas is null || Stamp is not { } stamp) return;

            Point position = e.GetPosition(_pageCanvas);

            // En cuanto hay movimiento de verdad, el historial guarda como estaba.
            if (!_moved)
            {
                _moved = true;
                FindMainViewModel()?.BeginTextStampMove();
            }

            double maxLeft = Math.Max(0, _pageCanvas.ActualWidth - ActualWidth);
            double maxTop = Math.Max(0, _pageCanvas.ActualHeight - ActualHeight);

            stamp.Left = Math.Clamp(position.X - _grabOffset.X, 0, maxLeft);
            stamp.Top = Math.Clamp(position.Y - _grabOffset.Y, 0, maxTop);
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;

            _isDragging = false;
            ReleaseMouseCapture();
        }

        /// <summary>
        /// Canvas de la pagina. Se busca el primero con tamano porque el panel del
        /// ItemsControl tambien es un Canvas, pero mide cero.
        /// </summary>
        private Canvas? FindPageCanvas()
        {
            DependencyObject? current = VisualTreeHelper.GetParent(this);

            while (current is not null)
            {
                if (current is Canvas canvas && canvas.ActualWidth > 0 && canvas.ActualHeight > 0) return canvas;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private MainViewModel? FindMainViewModel() => Window.GetWindow(this)?.DataContext as MainViewModel;

        #endregion

        /// <summary>
        /// Renderiza el texto tal y como se ve. Solo se renderiza el cuadro de
        /// texto: asi la barra de herramientas no acaba en el PDF y no hay que
        /// ocultarla (que era lo que dejaba el control sin barra al guardar).
        /// </summary>
        public RenderedText? RenderPng()
        {
            if (TxtInput.ActualWidth <= 0 || TxtInput.ActualHeight <= 0) return null;

            Point offset = TxtInput.TransformToAncestor(this).Transform(new Point(0, 0));

            RenderTargetBitmap bitmap = new(
                (int)Math.Ceiling(TxtInput.ActualWidth),
                (int)Math.Ceiling(TxtInput.ActualHeight),
                96,
                96,
                PixelFormats.Pbgra32);

            bitmap.Render(TxtInput);

            return new RenderedText(BitmapHelpers.ToPng(bitmap), offset.X, offset.Y);
        }
    }
}
