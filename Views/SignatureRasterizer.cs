using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pdf_Signer.Views
{
    /// <summary>
    /// Convierte los trazos del lienzo de firma en una imagen. Vive aparte de la
    /// ventana para poder comprobar el tamano del resultado sin abrir dialogos.
    /// </summary>
    internal static class SignatureRasterizer
    {
        /// <summary>Tamano de referencia de la firma, en unidades de dibujo.</summary>
        public const double ReferenceWidth = 200;
        public const double ReferenceHeight = 80;

        /// <summary>
        /// Pixeles por unidad de dibujo. La firma se estampa sobre unos 140
        /// puntos, asi que a 1x se verian los pixeles; a 4x queda en unos 400 ppp.
        /// </summary>
        public const int CaptureScale = 4;

        /// <summary>Margen alrededor del trazo, en unidades de dibujo.</summary>
        private const double Margin = 5;

        /// <summary>
        /// Rasteriza los trazos, o devuelve <c>null</c> si no ocupan area (un
        /// punto suelto no se puede convertir en imagen).
        /// </summary>
        public static BitmapSource? Render(StrokeCollection strokes)
        {
            ArgumentNullException.ThrowIfNull(strokes);

            Rect bounds = strokes.GetBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0) return null;

            double scale = Math.Min(ReferenceWidth / bounds.Width, ReferenceHeight / bounds.Height);

            double logicalWidth = Math.Max(1, Math.Ceiling(bounds.Width * scale) + (Margin * 2));
            double logicalHeight = Math.Max(1, Math.Ceiling(bounds.Height * scale) + (Margin * 2));

            DrawingVisual visual = new();

            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.PushTransform(new TranslateTransform((-bounds.X * scale) + Margin, (-bounds.Y * scale) + Margin));
                dc.PushTransform(new ScaleTransform(scale, scale));

                strokes.Draw(dc);

                dc.Pop();
                dc.Pop();
            }

            // Con los ppp a 96 * CaptureScale, WPF pinta el dibujo ampliado: el
            // trazo conserva su tamano relativo y gana muchos mas pixeles.
            RenderTargetBitmap bitmap = new(
                (int)Math.Ceiling(logicalWidth * CaptureScale),
                (int)Math.Ceiling(logicalHeight * CaptureScale),
                96 * CaptureScale,
                96 * CaptureScale,
                PixelFormats.Pbgra32);

            bitmap.Render(visual);
            bitmap.Freeze();

            return bitmap;
        }
    }
}
