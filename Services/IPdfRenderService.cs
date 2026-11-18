using System.Windows.Media.Imaging;

namespace Pdf_Signer.Services
{
    /// <summary>
    /// Lo que el ViewModel necesita del motor de PDF: cuantas paginas hay y como
    /// se ve una. Se abstrae para poder probar el ViewModel sin PDFs de verdad.
    /// </summary>
    public interface IPdfRenderService
    {
        /// <summary>Numero de paginas del documento.</summary>
        int GetPageCount(string pdfPath);

        /// <summary>Renderiza una pagina a 72 ppp, que es 1 pixel por punto.</summary>
        RenderedPage RenderPage(string pdfPath, int pageIndex);
    }

    /// <summary>
    /// Pagina ya renderizada. <see cref="WidthPoints"/> y
    /// <see cref="HeightPoints"/> son el tamano real en puntos PDF, que es el que
    /// se usa para colocar las cosas sobre el papel.
    /// </summary>
    public sealed record RenderedPage(BitmapImage Image, double WidthPoints, double HeightPoints);
}
