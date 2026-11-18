namespace Pdf_Signer.Core.Models;

/// <summary>
/// Firma lista para estampar en el PDF. Se guarda ya codificada en PNG porque
/// el PDF no entiende de objetos WPF y asi el nucleo se puede probar sin UI.
/// </summary>
public sealed record SignatureStamp
{
    /// <summary>Contenido del PNG de la firma.</summary>
    public required byte[] PngBytes { get; init; }

    /// <summary>Centro de la firma, en coordenadas relativas a la pagina.</summary>
    public required RelativePoint Center { get; init; }

    /// <summary>Pagina destino, indexada desde cero.</summary>
    public int TargetPage { get; init; }

    /// <summary>Ancho con el que se estampa, en puntos PDF.</summary>
    public double WidthPoints { get; init; } = Placement.SignaturePlacement.DefaultWidthPoints;
}
