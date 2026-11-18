using System.Buffers.Binary;

namespace Pdf_Signer.Core.Pdf;

/// <summary>
/// Lee el tamano de un PNG de su cabecera. Se hace a mano porque el nucleo no
/// puede depender de System.Drawing ni de WPF, y porque asi el estampado
/// necesita una sola fuente de verdad para la proporcion de la imagen.
/// </summary>
public readonly record struct PngSize(int Width, int Height)
{
    private static readonly byte[] Header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Alto dividido entre ancho.</summary>
    public double AspectRatio => (double)Height / Width;

    /// <summary>
    /// Lee ancho y alto de la cabecera IHDR.
    /// </summary>
    /// <exception cref="InvalidDataException">Si el contenido no es un PNG valido.</exception>
    public static PngSize Read(ReadOnlySpan<byte> png)
    {
        // 8 bytes de firma + 4 de longitud + 4 de tipo + 8 de ancho/alto.
        if (png.Length < 24 || !png[..8].SequenceEqual(Header))
            throw new InvalidDataException("El contenido no es un PNG.");

        if (!png.Slice(12, 4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException("El PNG no empieza por el bloque IHDR.");

        int width = BinaryPrimitives.ReadInt32BigEndian(png.Slice(16, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(png.Slice(20, 4));

        if (width <= 0 || height <= 0)
            throw new InvalidDataException($"El PNG tiene un tamano invalido ({width}x{height}).");

        return new PngSize(width, height);
    }
}
