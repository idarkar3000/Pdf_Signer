namespace Pdf_Signer.Services
{
    /// <summary>
    /// Texto ya renderizado a PNG, junto con el desplazamiento del texto dentro
    /// del control que lo contiene. Sin ese desplazamiento el texto estampado
    /// saldria corrido respecto a lo que ve el usuario, porque el control incluye
    /// la barra de herramientas.
    /// </summary>
    /// <param name="Png">Contenido del PNG.</param>
    /// <param name="OffsetX">Distancia horizontal del texto al borde del control, en pixeles.</param>
    /// <param name="OffsetY">Distancia vertical del texto al borde del control, en pixeles.</param>
    public sealed record RenderedText(byte[] Png, double OffsetX, double OffsetY);
}
