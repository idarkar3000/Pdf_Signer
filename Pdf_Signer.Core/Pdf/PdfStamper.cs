using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Placement;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace Pdf_Signer.Core.Pdf;

/// <summary>
/// Estampa la firma y los cuadros de texto sobre un PDF y devuelve el PDF
/// resultante. No toca el fichero original: entra un byte[] y sale otro.
/// </summary>
public static class PdfStamper
{
    /// <summary>
    /// Devuelve una copia del PDF con todo lo que hay en
    /// <paramref name="state"/> dibujado encima.
    /// </summary>
    public static byte[] Stamp(byte[] pdfBytes, EditorState state)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(state);

        PdfDocumentBuilder builder = new();

        using (MemoryStream input = new(pdfBytes))
        using (PdfDocument original = PdfDocument.Open(input))
        {
            for (int pageNumber = 1; pageNumber <= original.NumberOfPages; pageNumber++)
            {
                PdfPageBuilder pageBuilder = builder.AddPage(original, pageNumber);
                var page = original.GetPage(pageNumber);

                // El estado guarda las paginas desde cero; PdfPig las cuenta desde uno.
                int pageIndex = pageNumber - 1;

                if (state.Signature is { } signature && signature.TargetPage == pageIndex)
                {
                    StampSignature(pageBuilder, signature, page.Width, page.Height);
                }

                foreach (TextStamp text in state.TextStamps)
                {
                    if (text.TargetPage == pageIndex && !text.IsEmpty)
                    {
                        StampText(pageBuilder, text, page.Width, page.Height);
                    }
                }
            }
        }

        return builder.Build();
    }

    private static void StampSignature(PdfPageBuilder pageBuilder, SignatureStamp signature, double pageWidth, double pageHeight)
    {
        PngSize size = PngSize.Read(signature.PngBytes);

        PlacementRect rect = SignaturePlacement.Compute(
            signature.Center,
            signature.WidthPoints,
            size.AspectRatio,
            pageWidth,
            pageHeight);

        pageBuilder.AddPng(signature.PngBytes, rect.ToPdfRectangle(pageHeight));
    }

    private static void StampText(PdfPageBuilder pageBuilder, TextStamp text, double pageWidth, double pageHeight)
    {
        // El cuadro se coloca por su esquina, pero el PNG solo lleva el texto:
        // hay que sumarle el desfase del texto dentro del cuadro.
        PlacementRect rect = TextPlacement.ComputeAt(
            (text.TopLeft.X * pageWidth) + text.OffsetXPoints,
            (text.TopLeft.Y * pageHeight) + text.OffsetYPoints,
            text.WidthPoints,
            text.HeightPoints,
            pageWidth,
            pageHeight);

        pageBuilder.AddPng(text.PngBytes, rect.ToPdfRectangle(pageHeight));
    }
}
