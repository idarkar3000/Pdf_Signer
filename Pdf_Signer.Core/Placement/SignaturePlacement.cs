using Pdf_Signer.Core.Models;

namespace Pdf_Signer.Core.Placement;

/// <summary>
/// Convierte "el usuario ha hecho clic aqui" en un rectangulo dentro de la
/// pagina. Todo el calculo vive aqui para poder probarlo sin abrir una ventana.
/// </summary>
public static class SignaturePlacement
{
    public const double DefaultWidthPoints = 140.0;
    public const double MinWidthPoints = 30.0;
    public const double MaxWidthPoints = 600.0;

    public static double ClampWidth(double widthPoints) =>
        Math.Clamp(widthPoints, MinWidthPoints, MaxWidthPoints);

    /// <summary>
    /// Coloca la firma centrada en <paramref name="center"/> y la mantiene dentro
    /// de la pagina. Si la firma es mas grande que la pagina se reduce
    /// manteniendo la proporcion, en lugar de recortarse.
    /// </summary>
    /// <param name="center">Centro deseado, en coordenadas relativas (0..1).</param>
    /// <param name="widthPoints">Ancho deseado en puntos PDF.</param>
    /// <param name="aspectRatio">Alto / ancho de la imagen.</param>
    /// <param name="pageWidth">Ancho de la pagina en puntos PDF.</param>
    /// <param name="pageHeight">Alto de la pagina en puntos PDF.</param>
    public static PlacementRect Compute(
        RelativePoint center,
        double widthPoints,
        double aspectRatio,
        double pageWidth,
        double pageHeight)
    {
        if (pageWidth <= 0 || pageHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageWidth), "La pagina tiene que tener un tamano positivo.");
        if (aspectRatio <= 0 || double.IsNaN(aspectRatio))
            throw new ArgumentOutOfRangeException(nameof(aspectRatio), "La proporcion de la imagen tiene que ser positiva.");

        double width = ClampWidth(widthPoints);
        double height = width * aspectRatio;

        // Nunca estampar mas grande que la pagina: se encoge y se conserva la
        // proporcion. Antes esto reventaba porque el rango del Clamp quedaba
        // invertido (maximo < minimo) cuando la firma era mas ancha que la hoja.
        if (width > pageWidth)
        {
            width = pageWidth;
            height = width * aspectRatio;
        }

        if (height > pageHeight)
        {
            height = pageHeight;
            width = height / aspectRatio;
        }

        double left = Math.Clamp(
            (center.X * pageWidth) - (width / 2.0),
            0.0,
            Math.Max(0.0, pageWidth - width));

        double top = Math.Clamp(
            (center.Y * pageHeight) - (height / 2.0),
            0.0,
            Math.Max(0.0, pageHeight - height));

        return new PlacementRect(left, top, width, height);
    }
}
