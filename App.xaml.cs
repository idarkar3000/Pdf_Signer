using System.Windows;

namespace Pdf_Signer
{
    /// <summary>
    /// Punto de entrada. La ventana se crea aqui y no con <c>StartupUri</c> para
    /// que el <c>DataContext</c> se monte en un solo sitio y sea facil de cambiar
    /// por uno falso al probar.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow window = new();
            MainWindow = window;
            window.Show();
        }
    }
}
