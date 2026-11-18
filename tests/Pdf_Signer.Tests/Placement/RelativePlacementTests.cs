using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Placement;

namespace Pdf_Signer.Tests.Placement;

public class RelativePlacementTests
{
    [Fact]
    public void Pasa_Un_Punto_Del_Lienzo_A_Coordenadas_Relativas()
    {
        RelativePoint punto = RelativePlacement.FromCanvas(x: 297.5, y: 420.5, canvasWidth: 595, canvasHeight: 841);

        Assert.Equal(0.5, punto.X, 6);
        Assert.Equal(0.5, punto.Y, 6);
    }

    [Fact]
    public void Recorta_Los_Puntos_Que_Se_Salen_Del_Lienzo()
    {
        Assert.Equal(new RelativePoint(0, 0), RelativePlacement.FromCanvas(-50, -50, 595, 841));
        Assert.Equal(new RelativePoint(1, 1), RelativePlacement.FromCanvas(900, 900, 595, 841));
    }

    [Fact]
    public void Arrastrar_Mueve_En_Proporcion_Al_Lienzo()
    {
        RelativePoint movido = RelativePlacement.MoveBy(
            new RelativePoint(0.5, 0.5), deltaX: 59.5, deltaY: 84.1, canvasWidth: 595, canvasHeight: 841);

        Assert.Equal(0.6, movido.X, 6);
        Assert.Equal(0.6, movido.Y, 6);
    }

    [Fact]
    public void Arrastrar_Nunca_Saca_El_Punto_Del_Lienzo()
    {
        RelativePoint movido = RelativePlacement.MoveBy(
            new RelativePoint(0.5, 0.5), deltaX: -5000, deltaY: 5000, canvasWidth: 595, canvasHeight: 841);

        Assert.Equal(0, movido.X, 6);
        Assert.Equal(1, movido.Y, 6);
    }

    [Fact]
    public void Calcula_La_Esquina_Superior_Izquierda_De_Algo_Centrado()
    {
        (double left, double top) = RelativePlacement.ToCanvasTopLeft(
            RelativePoint.Center, width: 100, height: 50, canvasWidth: 600, canvasHeight: 800);

        Assert.Equal(250, left, 6);
        Assert.Equal(375, top, 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_Lienzos_Sin_Tamano(double canvasSize) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RelativePlacement.FromCanvas(0, 0, canvasSize, canvasSize));
}
