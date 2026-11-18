using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using PdfiumViewer;

namespace Pdf_Signer.Services
{
    /// <summary>
    /// Renderizado real con Pdfium. Se abre el PDF desde memoria en cada llamada
    /// para no dejar el fichero bloqueado mientras el usuario lo esta usando.
    /// </summary>
    public sealed class PdfiumRenderService : IPdfRenderService
    {
        /// <summary>Resolucion de render. A 72 ppp un pixel equivale a un punto PDF.</summary>
        private const int RenderDpi = 72;

        public int GetPageCount(string pdfPath)
        {
            using MemoryStream stream = ReadIntoMemory(pdfPath);
            using PdfDocument document = PdfDocument.Load(stream);

            return document.PageCount;
        }

        public RenderedPage RenderPage(string pdfPath, int pageIndex)
        {
            using MemoryStream stream = ReadIntoMemory(pdfPath);
            using PdfDocument document = PdfDocument.Load(stream);

            if (pageIndex < 0 || pageIndex >= document.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, "La pagina no existe en el documento.");

            using Image image = document.Render(pageIndex, RenderDpi, RenderDpi, true);
            using MemoryStream png = new();
            image.Save(png, ImageFormat.Png);
            png.Position = 0;

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = png;
            bitmap.EndInit();
            bitmap.Freeze();

            SizeF pageSize = document.PageSizes[pageIndex];

            return new RenderedPage(bitmap, pageSize.Width, pageSize.Height);
        }

        private static MemoryStream ReadIntoMemory(string pdfPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
            return new MemoryStream(File.ReadAllBytes(pdfPath));
        }
    }
}
