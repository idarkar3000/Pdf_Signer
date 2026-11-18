namespace Pdf_Signer.Core.Models;

/// <summary>
/// Cuadro de texto listo para estampar. Igual que la firma, se guarda como PNG
/// porque asi se conservan el color y la fuente tal y como se ven en pantalla.
/// Lleva tambien el texto, el tamano y el color de origen para poder reconstruir
/// el cuadro al deshacer.
/// </summary>
public sealed record TextStamp
{
    public required string Text { get; init; }

    /// <summary>Contenido del PNG con el texto ya renderizado.</summary>
    public required byte[] PngBytes { get; init; }

    /// <summary>Esquina superior izquierda del cuadro, relativa a la pagina.</summary>
    public required RelativePoint TopLeft { get; init; }

    /// <summary>Ancho del cuadro en puntos PDF.</summary>
    public double WidthPoints { get; init; }

    /// <summary>Alto del cuadro en puntos PDF.</summary>
    public double HeightPoints { get; init; }

    /// <summary>Tamano de fuente con el que se escribio.</summary>
    public double FontSize { get; init; }

    /// <summary>Color del texto en formato ARGB, para no depender de WPF.</summary>
    public int ColorArgb { get; init; }

    /// <summary>Pagina destino, indexada desde cero.</summary>
    public int TargetPage { get; init; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
}
