using System.IO;
using System.Windows.Media.Imaging;

namespace Pdf_Signer.Services
{
    /// <summary>
    /// Conversiones entre <see cref="BitmapImage"/> y PNG en memoria. El nucleo
    /// trabaja con bytes para no depender de WPF; aqui esta la frontera.
    /// </summary>
    public static class BitmapHelpers
    {
        /// <summary>Codifica un bitmap como PNG.</summary>
        public static byte[] ToPng(BitmapSource image)
        {
            ArgumentNullException.ThrowIfNull(image);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using MemoryStream stream = new();
            encoder.Save(stream);
            return stream.ToArray();
        }

        /// <summary>Decodifica un PNG en memoria. El bitmap queda congelado, asi que se puede usar desde cualquier hilo.</summary>
        public static BitmapImage FromPng(byte[] png)
        {
            ArgumentNullException.ThrowIfNull(png);

            using MemoryStream stream = new(png);

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
    }
}
