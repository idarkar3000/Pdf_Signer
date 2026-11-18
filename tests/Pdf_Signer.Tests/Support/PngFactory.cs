using System.Buffers.Binary;
using System.IO.Compression;

namespace Pdf_Signer.Tests.Support;

/// <summary>
/// Fabrica PNGs de verdad para las pruebas, sin dependencias externas. Se
/// necesita un PNG valido porque el estampado lo descomprime para meterlo en el
/// PDF, asi que un array de bytes cualquiera no vale.
/// </summary>
public static class PngFactory
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Crea un PNG opaco de color plano.</summary>
    public static byte[] Create(int width, int height, byte red = 255, byte green = 0, byte blue = 0)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        // Cada fila empieza por su byte de filtro (0 = sin filtro) y luego los
        // pixeles en RGB, que es el formato de color 2.
        int stride = (width * 3) + 1;
        byte[] raw = new byte[stride * height];

        for (int y = 0; y < height; y++)
        {
            int row = y * stride;
            raw[row] = 0;

            for (int x = 0; x < width; x++)
            {
                int pixel = row + 1 + (x * 3);
                raw[pixel] = red;
                raw[pixel + 1] = green;
                raw[pixel + 2] = blue;
            }
        }

        using MemoryStream compressed = new();

        using (ZLibStream deflate = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(raw);
        }

        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8;   // bits por canal
        header[9] = 2;   // RGB sin alfa
        header[10] = 0;  // compresion deflate
        header[11] = 0;  // filtro adaptativo
        header[12] = 0;  // sin entrelazado

        using MemoryStream png = new();
        png.Write(Signature);
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);

        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        byte[] crcInput = new byte[typeBytes.Length + data.Length];
        typeBytes.CopyTo(crcInput, 0);
        data.CopyTo(crcInput, typeBytes.Length);

        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(crcInput));
        stream.Write(crc);
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;

        foreach (byte value in data)
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }
}
