using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Placement;

namespace Pdf_Signer.Tests.Placement;

/// <summary>
/// Colocacion de la firma. Aqui vivia el fallo del <c>Clamp</c> con el rango
/// invertido, que reventaba cuando la firma era mas grande que la pagina.
/// </summary>
public class SignaturePlacementTests
{
    private const double PageWidth = 600;
    private const double PageHeight = 800;

    [Fact]
    public void Centra_La_Firma_En_El_Punto_Indicado()
    {
        PlacementRect rect = SignaturePlacement.Compute(
            RelativePoint.Center, widthPoints: 100, aspectRatio: 0.5, PageWidth, PageHeight);

        Assert.Equal(250, rect.Left, 6);
        Assert.Equal(375, rect.Top, 6);
        Assert.Equal(100, rect.Width, 6);
        Assert.Equal(50, rect.Height, 6);
    }

    [Fact]
    public void Recorta_Contra_El_Borde_Superior_Izquierdo()
    {
        PlacementRect rect = SignaturePlacement.Compute(
            new RelativePoint(0, 0), widthPoints: 100, aspectRatio: 0.5, PageWidth, PageHeight);

        Assert.Equal(0, rect.Left, 6);
        Assert.Equal(0, rect.Top, 6);
    }

    [Fact]
    public void Recorta_Contra_El_Borde_Inferior_Derecho()
    {
        PlacementRect rect = SignaturePlacement.Compute(
            new RelativePoint(1, 1), widthPoints: 100, aspectRatio: 0.5, PageWidth, PageHeight);

        Assert.Equal(PageWidth, rect.Right, 6);
        Assert.Equal(PageHeight, rect.Bottom, 6);
    }

    [Fact]
    public void Encoge_La_Firma_Si_Es_Mas_Ancha_Que_La_Pagina()
    {
        // Antes esto lanzaba ArgumentException porque el rango del Clamp quedaba
        // al reves (maximo < minimo).
        PlacementRect rect = SignaturePlacement.Compute(
            RelativePoint.Center, widthPoints: 5000, aspectRatio: 0.5, pageWidth: 300, pageHeight: 400);

        Assert.Equal(300, rect.Width, 6);
        Assert.Equal(150, rect.Height, 6);
        Assert.Equal(0, rect.Left, 6);
        Assert.Equal(125, rect.Top, 6);
    }

    [Fact]
    public void Encoge_La_Firma_Si_Es_Mas_Alta_Que_La_Pagina()
    {
        PlacementRect rect = SignaturePlacement.Compute(
            RelativePoint.Center, widthPoints: 100, aspectRatio: 10, pageWidth: 600, pageHeight: 400);

        Assert.Equal(400, rect.Height, 6);
        Assert.Equal(40, rect.Width, 6);
        Assert.Equal(280, rect.Left, 6);
        Assert.Equal(0, rect.Top, 6);
    }

    [Fact]
    public void El_Eje_Y_Crece_Hacia_Abajo()
    {
        PlacementRect arriba = SignaturePlacement.Compute(
            new RelativePoint(0.5, 0.2), widthPoints: 100, aspectRatio: 0.5, PageWidth, PageHeight);

        PlacementRect abajo = SignaturePlacement.Compute(
            new RelativePoint(0.5, 0.8), widthPoints: 100, aspectRatio: 0.5, PageWidth, PageHeight);

        Assert.True(arriba.Top < abajo.Top);
        Assert.Equal(160 - 25, arriba.Top, 6);
        Assert.Equal(640 - 25, abajo.Top, 6);
    }

    [Theory]
    [InlineData(1, SignaturePlacement.MinWidthPoints)]
    [InlineData(9999, SignaturePlacement.MaxWidthPoints)]
    [InlineData(140, 140)]
    public void Limita_El_Ancho_Al_Rango_Permitido(double entrada, double esperado) =>
        Assert.Equal(esperado, SignaturePlacement.ClampWidth(entrada), 6);

    [Theory]
    [InlineData(0, 800)]
    [InlineData(600, 0)]
    [InlineData(-1, -1)]
    public void Rechaza_Paginas_Sin_Tamano(double pageWidth, double pageHeight) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SignaturePlacement.Compute(RelativePoint.Center, 100, 0.5, pageWidth, pageHeight));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Rechaza_Proporciones_Invalidas(double aspectRatio) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SignaturePlacement.Compute(RelativePoint.Center, 100, aspectRatio, PageWidth, PageHeight));
}
