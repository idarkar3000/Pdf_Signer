using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PdfEditor.Views
{
    public partial class DraggableTextBlock : UserControl
    {
        private bool _isDragging;
        private Point _clickPosition;
        private Color _selectedColor = Colors.Red;

        public DraggableTextBlock()
        {
            InitializeComponent();
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e) => ShowControls(true);
        private void TxtInput_GotFocus(object sender, RoutedEventArgs e) => ShowControls(true);
        private void UserControl_LostFocus(object sender, RoutedEventArgs e) => ShowControls(false);

        public void ShowControls(bool visible)
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

        private void TxtFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtInput == null) return;
            if (double.TryParse(TxtFontSize.Text, out double newSize) && newSize > 4 && newSize < 200)
            {
                TxtInput.FontSize = newSize;
                TxtPlaceholder.FontSize = newSize;
            }
        }

        private void BtnPickColor_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();
            var colors = new (string Name, Color Color)[]
            {
                ("Red", Colors.Red),
                ("Black", Colors.Black),
                ("Blue", Colors.DeepSkyBlue),
                ("Green", Colors.LimeGreen),
                ("Orange", Colors.Orange),
                ("Purple", Colors.MediumPurple),
                ("White", Colors.White)
            };

            foreach (var (name, color) in colors)
            {
                var item = new MenuItem
                {
                    Header = name,
                    Icon = new Border { Width = 12, Height = 12, Background = new SolidColorBrush(color), CornerRadius = new CornerRadius(2) }
                };
                item.Click += (s, args) =>
                {
                    _selectedColor = color;
                    TxtInput.Foreground = new SolidColorBrush(_selectedColor);
                    ColorPreview.Fill = TxtInput.Foreground; // Uso correcto de Fill en Ellipse
                };
                contextMenu.Items.Add(item);
            }
            contextMenu.IsOpen = true;
        }

        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtPlaceholder.Visibility = string.IsNullOrEmpty(TxtInput.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Focus();
            ShowControls(true);

            if (e.OriginalSource is TextBox || e.OriginalSource is Button) return;

            _isDragging = true;
            _clickPosition = e.GetPosition(this);
            this.CaptureMouse();
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var parentCanvas = this.Parent as Canvas;
            if (parentCanvas == null) return;

            Point currentMousePosition = e.GetPosition(parentCanvas);
            double newLeft = currentMousePosition.X - _clickPosition.X;
            double newTop = currentMousePosition.Y - _clickPosition.Y;

            if (newLeft >= 0 && newLeft + this.ActualWidth <= parentCanvas.ActualWidth) Canvas.SetLeft(this, newLeft);
            if (newTop >= 0 && newTop + this.ActualHeight <= parentCanvas.ActualHeight) Canvas.SetTop(this, newTop);
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.ReleaseMouseCapture();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var parentCanvas = this.Parent as Canvas;
            parentCanvas?.Children.Remove(this);
        }

        public BitmapImage RenderToBitmap()
        {
            ShowControls(false);
            this.UpdateLayout();

            double width = TxtInput.ActualWidth + 12;
            double height = TxtInput.ActualHeight + 8;

            if (width <= 0 || height <= 0) return null;

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)Math.Max(1, width),
                (int)Math.Max(1, height),
                96, 96,
                PixelFormats.Pbgra32);

            rtb.Render(TxtInput);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                ms.Position = 0;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                return bitmap;
            }
        }

        public string Text => TxtInput.Text;
    }
}