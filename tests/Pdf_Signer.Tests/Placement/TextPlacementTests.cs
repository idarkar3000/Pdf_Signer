using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Placement;

namespace Pdf_Signer.Tests.Placement;

public class TextPlacementTests
{
    private const double PageWidth = 600;
    private const double PageHeight = 800;

    [Fact]
    public void Coloca_El_Cuadro_En_La_Posicion_Relativa()
    {
        PlacementRect rect = TextPlacement.Compute(
            new RelativePoint(0.5, 0.25), widthPoints: 100, heightPoints: 20, PageWidth, PageHeight);

        Assert.Equal(300, rect.Left, 6);
        Assert.Equal(200, rect.Top, 6);
        Assert.Equal(100, rect.Width, 6);
        Assert.Equal(20, rect.Height, 6);
    }

    [Fact]
    public void Recorta_Contra_El_Borde_Derecho()
    {
        PlacementRect rect = TextPlacement.Compute(
            new RelativePoint(0.99, 0.5), widthPoints: 100, heightPoints: 20, PageWidth, PageHeight);

        Assert.Equal(PageWidth - 100, rect.Left, 6);
    }

    [Fact]
    public void Recorta_Contra_El_Borde_Inferior()
    {
        PlacementRect rect = TextPlacement.Compute(
            new RelativePoint(0.5, 0.99), widthPoints: 100, heightPoints: 20, PageWidth, PageHeight);

        Assert.Equal(PageHeight - 20, rect.Top, 6);
    }

    [Fact]
    public void No_Revienta_Si_El_Cuadro_Es_Mayor_Que_La_Pagina()
    {
        PlacementRect rect = TextPlacement.Compute(
            RelativePoint.Center, widthPoints: 900, heightPoints: 900, PageWidth, PageHeight);

        Assert.Equal(0, rect.Left, 6);
        Assert.Equal(0, rect.Top, 6);
    }

    [Fact]
    public void ComputeAt_Usa_Puntos_Directos()
    {
        PlacementRect rect = TextPlacement.ComputeAt(
            leftPoints: 42, topPoints: 24, widthPoints: 100, heightPoints: 20, PageWidth, PageHeight);

        Assert.Equal(42, rect.Left, 6);
        Assert.Equal(24, rect.Top, 6);
    }

    [Theory]
    [InlineData(0, 800, 100, 20)]
    [InlineData(600, 800, 0, 20)]
    [InlineData(600, 800, 100, 0)]
    public void Rechaza_Tamanos_Invalidos(double pageWidth, double pageHeight, double width, double height) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TextPlacement.Compute(RelativePoint.Center, width, height, pageWidth, pageHeight));
}
