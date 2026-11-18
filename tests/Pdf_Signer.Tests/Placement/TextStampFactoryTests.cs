using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Placement;
using Pdf_Signer.Tests.Support;

namespace Pdf_Signer.Tests.Placement;

public class TextStampFactoryTests
{
    private const int CanvasWidth = 595;
    private const int CanvasHeight = 841;

    [Fact]
    public void Crea_El_Sello_Con_La_Geometria_De_La_Pagina()
    {
        // A 72 ppp un pixel del lienzo es un punto de PDF, asi que el tamano del
        // PNG se traslada tal cual.
        byte[] png = PngFactory.Create(width: 120, height: 30);

        TextStamp? stamp = TextStampFactory.Create(
            "Hola", png, left: 59.5, top: 84.1, offsetXPoints: 8, offsetYPoints: 32.8,
            CanvasWidth, CanvasHeight, pageWidth: 595, pageHeight: 841,
            fontSize: 16, colorArgb: unchecked((int)0xFFFF0000), targetPage: 1);

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
    public void Escala_El_Tamano_Si_El_Lienzo_No_Es_La_Pagina()
    {
        // Lienzo renderizado al doble: 100 px de texto son 50 puntos.
        byte[] png = PngFactory.Create(width: 100, height: 40);

        TextStamp? stamp = TextStampFactory.Create(
            "Hola", png, 0, 0, 0, 0, canvasWidth: 1190, canvasHeight: 1682,
            pageWidth: 595, pageHeight: 841, fontSize: 16, colorArgb: 0, targetPage: 0);

        Assert.NotNull(stamp);
        Assert.Equal(50, stamp.WidthPoints, 6);
        Assert.Equal(20, stamp.HeightPoints, 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Devuelve_Null_Si_No_Hay_Texto(string text) =>
        Assert.Null(TextStampFactory.Create(
            text, PngFactory.Create(10, 10), 0, 0, 0, 0, CanvasWidth, CanvasHeight, 595, 841, 16, 0, 0));

    [Fact]
    public void Devuelve_Null_Si_No_Hay_Png() =>
        Assert.Null(TextStampFactory.Create(
            "Hola", null, 0, 0, 0, 0, CanvasWidth, CanvasHeight, 595, 841, 16, 0, 0));

    [Theory]
    [InlineData(0, 841)]
    [InlineData(595, 0)]
    public void Devuelve_Null_Si_El_Lienzo_No_Tiene_Tamano(double canvasWidth, double canvasHeight) =>
        Assert.Null(TextStampFactory.Create(
            "Hola", PngFactory.Create(10, 10), 0, 0, 0, 0, canvasWidth, canvasHeight, 595, 841, 16, 0, 0));
}
