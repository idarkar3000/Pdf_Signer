using Pdf_Signer.Core.Models;

namespace Pdf_Signer.Core.Placement;

/// <summary>
/// Convierte lo que hay en el lienzo (posicion en puntos y PNG renderizado) en
/// un <see cref="TextStamp"/> expresado en coordenadas de pagina. Esta aqui y no
/// en el ViewModel para poder probarlo sin WPF.
/// </summary>
public static class TextStampFactory
{
    /// <summary>
    /// Crea el sello de texto, o <c>null</c> si no hay nada que estampar (texto
    /// vacio, sin PNG, caja sin tamano o pagina sin tamano).
    /// </summary>
    /// <param name="text">Texto escrito.</param>
    /// <param name="png">PNG con el texto renderizado.</param>
    /// <param name="box">
    /// Caja del texto sobre la pagina, en puntos. Es el tamano que ocupa en el
    /// documento, no el de los pixeles del PNG: el PNG se captura ampliado para
    /// que se vea nitido, pero ocupa lo mismo en el papel.
    /// </param>
    /// <param name="offsetXPoints">Distancia del texto al borde del cuadro, en puntos PDF.</param>
    /// <param name="offsetYPoints">Distancia del texto al borde del cuadro, en puntos PDF.</param>
    /// <param name="pageWidth">Ancho de la pagina en puntos PDF.</param>
    /// <param name="pageHeight">Alto de la pagina en puntos PDF.</param>
    /// <param name="fontSize">Tamano de fuente usado.</param>
    /// <param name="colorArgb">Color del texto en ARGB.</param>
    /// <param name="targetPage">Pagina destino, desde cero.</param>
    public static TextStamp? Create(
        string text,
        byte[]? png,
        PlacementRect box,
        double offsetXPoints,
        double offsetYPoints,
        double pageWidth,
        double pageHeight,
        double fontSize,
        int colorArgb,
        int targetPage)
    {
        if (string.IsNullOrWhiteSpace(text) || png is null) return null;
        if (pageWidth <= 0 || pageHeight <= 0) return null;
        if (box.Width <= 0 || box.Height <= 0) return null;

        return new TextStamp
        {
            Text = text,
            PngBytes = png,
            TopLeft = new RelativePoint(box.Left / pageWidth, box.Top / pageHeight).Clamped(),
            OffsetXPoints = offsetXPoints,
            OffsetYPoints = offsetYPoints,
            WidthPoints = box.Width,
            HeightPoints = box.Height,
            FontSize = fontSize,
            ColorArgb = colorArgb,
            TargetPage = targetPage
        };
    }
}
