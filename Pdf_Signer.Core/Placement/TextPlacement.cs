using Pdf_Signer.Core.Models;

namespace Pdf_Signer.Core.Placement;

/// <summary>
/// Colocacion de los cuadros de texto. A diferencia de la firma, el texto se
/// ancla por su esquina superior izquierda y no se reescala: si no cabe se
/// recorta contra el borde, que es menos malo que deformarlo.
/// </summary>
public static class TextPlacement
{
    public static PlacementRect Compute(
        RelativePoint topLeft,
        double widthPoints,
        double heightPoints,
        double pageWidth,
        double pageHeight)
    {
        if (pageWidth <= 0 || pageHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageWidth), "La pagina tiene que tener un tamano positivo.");
        if (widthPoints <= 0 || heightPoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthPoints), "El cuadro de texto tiene que tener un tamano positivo.");

        // Math.Max(0, ...) para que un cuadro mas grande que la hoja no reviente
        // el Clamp con el rango invertido.
        double left = Math.Clamp(
            topLeft.X * pageWidth,
            0.0,
            Math.Max(0.0, pageWidth - widthPoints));

        double top = Math.Clamp(
            topLeft.Y * pageHeight,
            0.0,
            Math.Max(0.0, pageHeight - heightPoints));

        return new PlacementRect(left, top, widthPoints, heightPoints);
    }
}
