using System.Windows.Media.Imaging;

namespace Pdf_Signer.Services
{
    /// <summary>
    /// Todo lo que el ViewModel no puede hacer por si mismo porque necesita una
    /// ventana: elegir ficheros, pedir la firma y avisar al usuario.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Pide un PDF. Devuelve <c>null</c> si el usuario cancela.</summary>
        string? PickPdfToOpen();

        /// <summary>Pide donde guardar. Devuelve <c>null</c> si el usuario cancela.</summary>
        string? PickPdfToSave(string suggestedFileName);

        /// <summary>Abre el lienzo para dibujar la firma. Devuelve <c>null</c> si el usuario cancela.</summary>
        BitmapImage? DrawSignature();

        void ShowError(string message);

        void ShowInfo(string message);
    }
}
