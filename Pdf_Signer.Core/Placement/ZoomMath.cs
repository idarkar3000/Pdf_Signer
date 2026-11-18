namespace Pdf_Signer.Core.Placement;

/// <summary>
/// Cuentas del zoom. El lienzo se mide en puntos PDF y se pinta al doble de
/// tamano (<see cref="BaseScale"/>) para que se lea comodo en pantalla; el zoom
/// que ve el usuario es un multiplicador sobre esa base. La resolucion a la que
/// se rasteriza la pagina no entra aqui: el lienzo siempre se mide en puntos.
/// </summary>
public static class ZoomMath
{
    /// <summary>
    /// Pixeles de pantalla por punto con zoom al 100 %. Por eso "100 %" en la
    /// barra equivale a ver la pagina al doble de su tamano real.
    /// </summary>
    public const double BaseScale = 2.0;

    public const double MinZoom = 0.2;
    public const double MaxZoom = 4.0;

    /// <summary>Paso de los botones + y -.</summary>
    public const double ButtonStep = 0.25;

    /// <summary>Paso de la rueda con Ctrl pulsado.</summary>
    public const double WheelStep = 0.15;

    /// <summary>Margen vertical que se deja libre al ajustar la pagina a la ventana.</summary>
    public const double FitMargin = 60.0;

    public static double Clamp(double zoom) => Math.Clamp(zoom, MinZoom, MaxZoom);

    /// <summary>Escala real que se aplica al canvas.</summary>
    public static double EffectiveScale(double zoom) => BaseScale * Clamp(zoom);

    public static double ZoomIn(double zoom, double step = ButtonStep) => Clamp(zoom + step);

    public static double ZoomOut(double zoom, double step = ButtonStep) => Clamp(zoom - step);

    /// <summary>Zoom con la rueda: <paramref name="wheelDelta"/> es el delta del evento (positivo = acercar).</summary>
    public static double Wheel(double zoom, int wheelDelta, double step = WheelStep) =>
        wheelDelta > 0 ? Clamp(zoom + step)
        : wheelDelta < 0 ? Clamp(zoom - step)
        : Clamp(zoom);

    /// <summary>
    /// Zoom que hace que la pagina entera quepa en el alto disponible.
    /// </summary>
    /// <param name="availableHeight">Alto visible del visor, en pixeles.</param>
    /// <param name="pageHeightPoints">Alto de la pagina en puntos PDF.</param>
    public static double FitPage(double availableHeight, double pageHeightPoints)
    {
        if (pageHeightPoints <= 0) return MinZoom;

        double usableHeight = availableHeight - FitMargin;
        if (usableHeight <= 0) return MinZoom;

        return Clamp(usableHeight / (pageHeightPoints * BaseScale));
    }

    /// <summary>Texto del indicador de zoom. Es el zoom relativo, no el tamano real.</summary>
    public static string FormatPercent(double zoom) => $"{Math.Round(zoom * 100)}%";
}
