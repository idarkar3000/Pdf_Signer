namespace Pdf_Signer.Core.Models;

/// <summary>
/// Punto en coordenadas relativas a la pagina: (0,0) es la esquina superior
/// izquierda y (1,1) la inferior derecha. Asi la posicion no depende del zoom
/// ni del tamano al que se haya renderizado la vista previa.
/// </summary>
public readonly record struct RelativePoint(double X, double Y)
{
    public static RelativePoint Center { get; } = new(0.5, 0.5);

    public RelativePoint Clamped() => new(Math.Clamp(X, 0.0, 1.0), Math.Clamp(Y, 0.0, 1.0));
}
