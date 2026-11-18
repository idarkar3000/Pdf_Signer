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
        /// <summary>
        /// Resolucion de render. A 72 ppp un pixel seria un punto PDF; se
        /// rasteriza al triple para que la pagina se vea nitida al ampliar y
        /// para que lo que se estampa no salga borroso. El lienzo sigue
        /// midiendose en puntos, asi que subir esto no cambia ninguna coordenada.
        /// </summary>
        private const int RenderDpi = 216;

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

            SizeF pageSize = document.PageSizes[pageIndex];

            // Ojo: la sobrecarga Render(page, dpiX, dpiY, forPrinting) NO cambia el
            // tamano del bitmap, solo su metadato de resolucion (pinta la pagina en
            // un bitmap del tamano en puntos). Para rasterizar de verdad a mas
            // resolucion hay que pasarle el tamano en pixeles y los ppp.
            int pixelWidth = (int)Math.Round(pageSize.Width * RenderDpi / 72.0);
            int pixelHeight = (int)Math.Round(pageSize.Height * RenderDpi / 72.0);

            using Image image = document.Render(pageIndex, pixelWidth, pixelHeight, RenderDpi, RenderDpi, true);
            using MemoryStream png = new();
            image.Save(png, ImageFormat.Png);
            png.Position = 0;

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = png;
            bitmap.EndInit();
            bitmap.Freeze();

            return new RenderedPage(bitmap, pageSize.Width, pageSize.Height);
        }

        private static MemoryStream ReadIntoMemory(string pdfPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
            return new MemoryStream(File.ReadAllBytes(pdfPath));
        }
    }
}
