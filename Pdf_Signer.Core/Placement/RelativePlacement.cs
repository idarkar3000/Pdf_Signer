using Pdf_Signer.Core.Models;

namespace Pdf_Signer.Core.Placement;

/// <summary>
/// Traducciones entre las coordenadas de la vista (pixeles del canvas) y las
/// coordenadas relativas que se guardan en el documento.
/// </summary>
public static class RelativePlacement
{
    /// <summary>Pasa un punto del canvas a coordenadas relativas, recortando a la pagina.</summary>
    public static RelativePoint FromCanvas(double x, double y, double canvasWidth, double canvasHeight)
    {
        if (canvasWidth <= 0 || canvasHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(canvasWidth), "El canvas tiene que tener un tamano positivo.");

        return new RelativePoint(
            Math.Clamp(x / canvasWidth, 0.0, 1.0),
            Math.Clamp(y / canvasHeight, 0.0, 1.0));
    }

    /// <summary>
    /// Desplaza un punto relativo la distancia indicada en pixeles de canvas.
    /// Se usa al arrastrar: el resultado nunca sale de la pagina.
    /// </summary>
    public static RelativePoint MoveBy(
        RelativePoint start,
        double deltaX,
        double deltaY,
        double canvasWidth,
        double canvasHeight)
    {
        if (canvasWidth <= 0 || canvasHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(canvasWidth), "El canvas tiene que tener un tamano positivo.");

        return new RelativePoint(
            Math.Clamp(start.X + (deltaX / canvasWidth), 0.0, 1.0),
            Math.Clamp(start.Y + (deltaY / canvasHeight), 0.0, 1.0));
    }

    /// <summary>
    /// Esquina superior izquierda, en pixeles de canvas, de un elemento de tamano
    /// <paramref name="width"/> x <paramref name="height"/> centrado en
    /// <paramref name="center"/>.
    /// </summary>
    /// <remarks>El resultado puede ser negativo si el elemento es mayor que el canvas; aqui no se recorta.</remarks>
    public static (double Left, double Top) ToCanvasTopLeft(
        RelativePoint center,
        double width,
        double height,
        double canvasWidth,
        double canvasHeight) =>
        ((center.X * canvasWidth) - (width / 2.0), (center.Y * canvasHeight) - (height / 2.0));

    /// <summary>
    /// Pasa una medida del canvas a puntos PDF. Es lo que convierte el tamano en
    /// pixeles de un texto renderizado al tamano real sobre el papel.
    /// </summary>
    public static double CanvasToPdf(double canvasLength, double pdfLength, double canvasTotal)
    {
        if (canvasTotal <= 0)
            throw new ArgumentOutOfRangeException(nameof(canvasTotal), "El canvas tiene que tener un tamano positivo.");

        return canvasLength / canvasTotal * pdfLength;
    }
}
