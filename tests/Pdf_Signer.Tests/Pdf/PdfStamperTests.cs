using Pdf_Signer.Core.Models;
using Pdf_Signer.Core.Pdf;
using Pdf_Signer.Tests.Support;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Pdf_Signer.Tests.Pdf;

public class PdfStamperTests
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;

    [Fact]
    public void Estampa_La_Firma_Solo_En_Su_Pagina()
    {
        byte[] pdf = PdfFactory.CreateBlank(pages: 2);
        byte[] firma = PngFactory.Create(width: 200, height: 100);

        EditorState estado = new()
        {
            Signature = new SignatureStamp
            {
                PngBytes = firma,
                Center = RelativePoint.Center,
                TargetPage = 1,
                WidthPoints = 100
            }
        };

        using PdfDocument resultado = PdfDocument.Open(PdfStamper.Stamp(pdf, estado));

        Assert.Equal(2, resultado.NumberOfPages);
        Assert.Empty(resultado.GetPage(1).GetImages());

        IPdfImage imagen = Assert.Single(resultado.GetPage(2).GetImages());
        Assert.Equal(200, imagen.WidthInSamples);
        Assert.Equal(100, imagen.HeightInSamples);

        // Centrada y con 100 puntos de ancho: 50 puntos de alto por la proporcion.
        Assert.Equal(247.5, imagen.BoundingBox.Left, 1);
        Assert.Equal(396, imagen.BoundingBox.Bottom, 1);
        Assert.Equal(347.5, imagen.BoundingBox.Right, 1);
        Assert.Equal(446, imagen.BoundingBox.Top, 1);
    }

    [Fact]
    public void Estampa_El_Texto_Con_Su_Desfase()
    {
        byte[] pdf = PdfFactory.CreateBlank(pages: 1);

        EditorState estado = new()
        {
            TextStamps =
            [
                new TextStamp
                {
                    Text = "Hola",
                    PngBytes = PngFactory.Create(width: 120, height: 30, blue: 255),
                    TopLeft = new RelativePoint(0.1, 0.1),
                    OffsetXPoints = 8,
                    OffsetYPoints = 32.8,
                    WidthPoints = 120,
                    HeightPoints = 30,
                    FontSize = 16,
                    TargetPage = 0
                }
            ]
        };

        using PdfDocument resultado = PdfDocument.Open(PdfStamper.Stamp(pdf, estado));

        IPdfImage imagen = Assert.Single(resultado.GetPage(1).GetImages());

        // 0.1 * 595 = 59.5 mas 8 de desfase; 0.1 * 842 = 84.2 mas 32.8.
        Assert.Equal(67.5, imagen.BoundingBox.Left, 1);
        Assert.Equal(842 - 117 - 30, imagen.BoundingBox.Bottom, 1);
        Assert.Equal(187.5, imagen.BoundingBox.Right, 1);
        Assert.Equal(842 - 117, imagen.BoundingBox.Top, 1);
    }

    [Fact]
    public void Ignora_Los_Cuadros_De_Texto_Vacios()
    {
        byte[] pdf = PdfFactory.CreateBlank(pages: 1);

        EditorState estado = new()
        {
            TextStamps =
            [
                new TextStamp
                {
                    Text = "   ",
                    PngBytes = PngFactory.Create(width: 10, height: 10),
                    TopLeft = RelativePoint.Center,
                    WidthPoints = 10,
                    HeightPoints = 10
                }
            ]
        };

        using PdfDocument resultado = PdfDocument.Open(PdfStamper.Stamp(pdf, estado));

        Assert.Empty(resultado.GetPage(1).GetImages());
    }

    [Fact]
    public void Un_Estado_Vacio_Devuelve_El_Pdf_Tal_Cual()
    {
        byte[] pdf = PdfFactory.CreateBlank(pages: 2);

        using PdfDocument resultado = PdfDocument.Open(PdfStamper.Stamp(pdf, EditorState.Empty));

        Assert.Equal(2, resultado.NumberOfPages);
        Assert.Empty(resultado.GetPage(1).GetImages());
        Assert.Empty(resultado.GetPage(2).GetImages());
    }

    [Fact]
    public void No_Modifica_El_Pdf_De_Entrada()
    {
        byte[] pdf = PdfFactory.CreateBlank(pages: 1);
        byte[] copia = pdf.ToArray();

        EditorState estado = new()
        {
            Signature = new SignatureStamp
            {
                PngBytes = PngFactory.Create(width: 20, height: 10),
                Center = RelativePoint.Center,
                TargetPage = 0
            }
        };

        PdfStamper.Stamp(pdf, estado);

        Assert.Equal(copia, pdf);
    }
}
