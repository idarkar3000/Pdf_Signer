using Pdf_Signer.Core.Pdf;
using Pdf_Signer.Tests.Support;

namespace Pdf_Signer.Tests.Pdf;

public class PngSizeTests
{
    [Fact]
    public void Lee_El_Tamano_De_La_Cabecera()
    {
        PngSize size = PngSize.Read(PngFactory.Create(width: 7, height: 3));

        Assert.Equal(7, size.Width);
        Assert.Equal(3, size.Height);
        Assert.Equal(3.0 / 7.0, size.AspectRatio, 6);
    }

    [Fact]
    public void Rechaza_Contenido_Que_No_Es_Png()
    {
        byte[] noEsPng = new byte[64];

        Assert.Throws<InvalidDataException>(() => PngSize.Read(noEsPng));
    }

    [Fact]
    public void Rechaza_Un_Png_Truncado()
    {
        byte[] png = PngFactory.Create(width: 7, height: 3);

        Assert.Throws<InvalidDataException>(() => PngSize.Read(png.AsSpan(0, 10).ToArray()));
    }

    [Fact]
    public void Rechaza_Un_Png_Sin_Ihdr()
    {
        byte[] png = PngFactory.Create(width: 7, height: 3);
        png[12] = (byte)'X';

        Assert.Throws<InvalidDataException>(() => PngSize.Read(png));
    }
}
