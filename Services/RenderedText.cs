namespace Pdf_Signer.Services
{
    /// <summary>
    /// Texto ya renderizado a PNG, junto con el desplazamiento y el tamano del
    /// texto dentro del control que lo contiene. Sin esos datos el texto
    /// estampado saldria corrido o con el tamano equivocado, porque el control
    /// incluye la barra de herramientas y el PNG se captura ampliado.
    /// </summary>
    /// <param name="Png">Contenido del PNG, ya a resolucion de impresion.</param>
    /// <param name="OffsetX">Distancia horizontal del texto al borde del control, en puntos.</param>
    /// <param name="OffsetY">Distancia vertical del texto al borde del control, en puntos.</param>
    /// <param name="Width">Ancho del texto en puntos, no en pixeles del PNG.</param>
    /// <param name="Height">Alto del texto en puntos, no en pixeles del PNG.</param>
    public sealed record RenderedText(
        byte[] Png,
        double OffsetX,
        double OffsetY,
        double Width,
        double Height);
}
