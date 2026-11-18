using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Pdf_Signer.Views;

namespace Pdf_Signer.Services
{
    /// <summary>
    /// Implementacion real de <see cref="IDialogService"/> sobre las ventanas de
    /// WPF.
    /// </summary>
    public sealed class DialogService : IDialogService
    {
        public string? PickPdfToOpen()
        {
            OpenFileDialog dialog = new()
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Select a PDF File"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? PickPdfToSave(string suggestedFileName)
        {
            SaveFileDialog dialog = new()
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Save Signed PDF",
                FileName = suggestedFileName
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public BitmapImage? DrawSignature()
        {
            SignatureWindow window = new()
            {
                Owner = Application.Current?.MainWindow
            };

            return window.ShowDialog() == true ? window.SignatureBitmap : null;
        }

        public void ShowError(string message) =>
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        public void ShowInfo(string message) =>
            MessageBox.Show(message, "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
