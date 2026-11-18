using Pdf_Signer.Core.Models;
using UglyToad.PdfPig.Core;

namespace Pdf_Signer.Core.Pdf;

/// <summary>
/// Paso de coordenadas de pantalla (origen arriba a la izquierda) a coordenadas
/// de PDF (origen abajo a la izquierda).
/// </summary>
public static class PdfCoordinates
{
    /// <summary>
    /// Convierte un rectangulo medido desde arriba en el rectangulo equivalente
    /// en puntos PDF.
    /// </summary>
    public static PdfRectangle ToPdfRectangle(this PlacementRect rect, double pageHeight)
    {
        double bottom = pageHeight - rect.Top - rect.Height;
        double top = pageHeight - rect.Top;

        return new PdfRectangle(rect.Left, bottom, rect.Right, top);
    }
}
