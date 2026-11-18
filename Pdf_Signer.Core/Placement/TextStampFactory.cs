using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Pdf;

namespace Pdf_Signer.Core.Placement;

/// <summary>
/// Convierte lo que hay en el lienzo (posicion en pixeles y PNG renderizado) en
/// un <see cref="TextStamp"/> expresado en coordenadas de pagina. Esta aqui y no
/// en el ViewModel para poder probarlo sin WPF.
/// </summary>
public static class TextStampFactory
{
    /// <summary>
    /// Crea el sello de texto, o <c>null</c> si no hay nada que estampar (texto
    /// vacio, sin PNG o lienzo sin tamano).
    /// </summary>
    /// <param name="text">Texto escrito.</param>
    /// <param name="png">PNG con el texto renderizado.</param>
    /// <param name="left">Posicion del cuadro en el lienzo, en pixeles.</param>
    /// <param name="top">Posicion del cuadro en el lienzo, en pixeles.</param>
    /// <param name="canvasWidth">Ancho del lienzo en pixeles (la pagina renderizada).</param>
    /// <param name="canvasHeight">Alto del lienzo en pixeles.</param>
    /// <param name="pageWidth">Ancho de la pagina en puntos PDF.</param>
    /// <param name="pageHeight">Alto de la pagina en puntos PDF.</param>
    /// <param name="fontSize">Tamano de fuente usado.</param>
    /// <param name="colorArgb">Color del texto en ARGB.</param>
    /// <param name="targetPage">Pagina destino, desde cero.</param>
    public static TextStamp? Create(
        string text,
        byte[]? png,
        double left,
        double top,
        double canvasWidth,
        double canvasHeight,
        double pageWidth,
        double pageHeight,
        double fontSize,
        int colorArgb,
        int targetPage)
    {
        if (string.IsNullOrWhiteSpace(text) || png is null) return null;
        if (canvasWidth <= 0 || canvasHeight <= 0) return null;

        PngSize size = PngSize.Read(png);

        return new TextStamp
        {
            Text = text,
            PngBytes = png,
            TopLeft = new RelativePoint(left / canvasWidth, top / canvasHeight).Clamped(),
            WidthPoints = RelativePlacement.CanvasToPdf(size.Width, pageWidth, canvasWidth),
            HeightPoints = RelativePlacement.CanvasToPdf(size.Height, pageHeight, canvasHeight),
            FontSize = fontSize,
            ColorArgb = colorArgb,
            TargetPage = targetPage
        };
    }
}
