namespace Pdf_Signer.Core.Models;

/// <summary>
/// Rectangulo en puntos PDF (1 punto = 1/72 pulgada).
/// <see cref="Top"/> se mide desde el borde superior de la pagina, igual que en
/// pantalla; <see cref="Pdf.PdfStamper"/> es quien lo convierte al sistema de
/// coordenadas de PDF (origen abajo a la izquierda).
/// </summary>
public readonly record struct PlacementRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;

    public double Bottom => Top + Height;

    public bool Contains(double x, double y) =>
        x >= Left && x <= Right && y >= Top && y <= Bottom;
}
