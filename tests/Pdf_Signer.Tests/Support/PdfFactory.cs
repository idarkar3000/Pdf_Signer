using UglyToad.PdfPig.Writer;

namespace Pdf_Signer.Tests.Support;

/// <summary>PDFs en blanco para probar el estampado sin depender de ficheros.</summary>
public static class PdfFactory
{
    /// <summary>PDF de <paramref name="pages"/> paginas en blanco de tamano A4.</summary>
    public static byte[] CreateBlank(int pages, double width = 595, double height = 842)
    {
        if (pages <= 0) throw new ArgumentOutOfRangeException(nameof(pages));

        PdfDocumentBuilder builder = new();

        for (int page = 0; page < pages; page++)
        {
            builder.AddPage(width, height);
        }

        return builder.Build();
    }
}
