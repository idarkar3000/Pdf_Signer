using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Placement;
using Pdf_Signer.Tests.Support;

namespace Pdf_Signer.Tests.Placement;

public class TextStampFactoryTests
{
    private const double PageWidth = 595;
    private const double PageHeight = 841;

    [Fact]
    public void Crea_El_Sello_Con_La_Geometria_De_La_Caja()
    {
        TextStamp? stamp = TextStampFactory.Create(
            "Hola",
            PngFactory.Create(width: 480, height: 120),
            new PlacementRect(59.5, 84.1, 120, 30),
            offsetXPoints: 8,
            offsetYPoints: 32.8,
            PageWidth,
            PageHeight,
            fontSize: 16,
            colorArgb: unchecked((int)0xFFFF0000),
            targetPage: 1);

        Assert.NotNull(stamp);
        Assert.Equal(0.1, stamp.TopLeft.X, 6);
        Assert.Equal(0.1, stamp.TopLeft.Y, 6);
        Assert.Equal(120, stamp.WidthPoints, 6);
        Assert.Equal(30, stamp.HeightPoints, 6);
        Assert.Equal(8, stamp.OffsetXPoints, 6);
        Assert.Equal(32.8, stamp.OffsetYPoints, 6);
        Assert.Equal(16, stamp.FontSize, 6);
        Assert.Equal(1, stamp.TargetPage);
    }

    [Fact]
    public void El_Tamano_Del_Png_No_Cambia_El_Tamano_Estampado()
    {
        // El texto se captura ampliado (4x) para que no se vean los pixeles al
        // estamparlo. Eso no puede hacer que ocupe cuatro veces mas en el papel:
        // manda la caja, no los pixeles del PNG.
        TextStamp? normal = Create("Hola", PngFactory.Create(width: 120, height: 30));
        TextStamp? ampliado = Create("Hola", PngFactory.Create(width: 480, height: 120));

        Assert.NotNull(normal);
        Assert.NotNull(ampliado);
        Assert.Equal(normal.WidthPoints, ampliado.WidthPoints, 6);
        Assert.Equal(normal.HeightPoints, ampliado.HeightPoints, 6);
    }

    [Fact]
    public void Recorta_La_Posicion_A_La_Pagina()
    {
        TextStamp? stamp = TextStampFactory.Create(
            "Hola",
            PngFactory.Create(10, 10),
            new PlacementRect(-40, -40, 10, 10),
            0,
            0,
            PageWidth,
            PageHeight,
            fontSize: 16,
            colorArgb: 0,
            targetPage: 0);

        Assert.NotNull(stamp);
        Assert.Equal(0, stamp.TopLeft.X, 6);
        Assert.Equal(0, stamp.TopLeft.Y, 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Devuelve_Null_Si_No_Hay_Texto(string text) =>
        Assert.Null(Create(text, PngFactory.Create(10, 10)));

    [Fact]
    public void Devuelve_Null_Si_No_Hay_Png() =>
        Assert.Null(TextStampFactory.Create(
            "Hola",
            null,
            new PlacementRect(0, 0, 10, 10),
            0,
            0,
            PageWidth,
            PageHeight,
            fontSize: 16,
            colorArgb: 0,
            targetPage: 0));

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(-5, 10)]
    public void Devuelve_Null_Si_La_Caja_No_Tiene_Tamano(double width, double height) =>
        Assert.Null(TextStampFactory.Create(
            "Hola",
            PngFactory.Create(10, 10),
            new PlacementRect(0, 0, width, height),
            0,
            0,
            PageWidth,
            PageHeight,
            fontSize: 16,
            colorArgb: 0,
            targetPage: 0));

    [Theory]
    [InlineData(0, 841)]
    [InlineData(595, 0)]
    public void Devuelve_Null_Si_La_Pagina_No_Tiene_Tamano(double pageWidth, double pageHeight) =>
        Assert.Null(TextStampFactory.Create(
            "Hola",
            PngFactory.Create(10, 10),
            new PlacementRect(0, 0, 10, 10),
            0,
            0,
            pageWidth,
            pageHeight,
            fontSize: 16,
            colorArgb: 0,
            targetPage: 0));

    private static TextStamp? Create(string text, byte[] png) =>
        TextStampFactory.Create(
            text,
            png,
            new PlacementRect(100, 200, 120, 30),
            offsetXPoints: 8,
            offsetYPoints: 32.8,
            PageWidth,
            PageHeight,
            fontSize: 16,
            colorArgb: 0,
            targetPage: 0);
}
