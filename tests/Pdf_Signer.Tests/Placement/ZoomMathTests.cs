using Pdf_Signer.Core.Placement;

namespace Pdf_Signer.Tests.Placement;

public class ZoomMathTests
{
    [Theory]
    [InlineData(0.01, ZoomMath.MinZoom)]
    [InlineData(99, ZoomMath.MaxZoom)]
    [InlineData(1.5, 1.5)]
    public void Limita_El_Zoom_Al_Rango_Permitido(double entrada, double esperado) =>
        Assert.Equal(esperado, ZoomMath.Clamp(entrada), 6);

    [Fact]
    public void La_Escala_Real_Es_El_Doble_Del_Zoom() =>
        Assert.Equal(3.0, ZoomMath.EffectiveScale(1.5), 6);

    [Fact]
    public void Los_Botones_Suman_Y_Restan_Su_Paso()
    {
        Assert.Equal(1.25, ZoomMath.ZoomIn(1.0), 6);
        Assert.Equal(0.75, ZoomMath.ZoomOut(1.0), 6);
    }

    [Fact]
    public void Los_Botones_No_Se_Salen_Del_Rango()
    {
        Assert.Equal(ZoomMath.MaxZoom, ZoomMath.ZoomIn(ZoomMath.MaxZoom), 6);
        Assert.Equal(ZoomMath.MinZoom, ZoomMath.ZoomOut(ZoomMath.MinZoom), 6);
    }

    [Fact]
    public void La_Rueda_Acerca_Y_Aleja()
    {
        Assert.Equal(1.15, ZoomMath.Wheel(1.0, 120), 6);
        Assert.Equal(0.85, ZoomMath.Wheel(1.0, -120), 6);
        Assert.Equal(1.0, ZoomMath.Wheel(1.0, 0), 6);
    }

    [Fact]
    public void Ajustar_La_Pagina_Descuenta_El_Margen()
    {
        // (860 - 60) / (1000 * 2) = 0.4
        Assert.Equal(0.4, ZoomMath.FitPage(availableHeight: 860, pageHeightPoints: 1000), 6);
    }

    [Theory]
    [InlineData(860, 0)]
    [InlineData(50, 1000)]
    public void Ajustar_La_Pagina_Nunca_Baja_Del_Minimo(double availableHeight, double pageHeightPoints) =>
        Assert.Equal(ZoomMath.MinZoom, ZoomMath.FitPage(availableHeight, pageHeightPoints), 6);

    [Theory]
    [InlineData(1.0, "100%")]
    [InlineData(1.25, "125%")]
    [InlineData(0.2, "20%")]
    public void El_Indicador_Muestra_Porcentaje(double zoom, string esperado) =>
        Assert.Equal(esperado, ZoomMath.FormatPercent(zoom));
}
