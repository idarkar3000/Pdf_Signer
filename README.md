# Pdf_Signer

[![CI](https://github.com/idarkar3000/Pdf_Signer/actions/workflows/ci.yml/badge.svg)](https://github.com/idarkar3000/Pdf_Signer/actions/workflows/ci.yml)

Aplicación de escritorio en C# y WPF sobre .NET 10 para abrir un PDF y estampar encima una firma dibujada a mano y cuadros de texto.

La hice para firmar y anotar documentos sin pasar por la impresora ni el escáner.

## Qué hace

- **Visor**: renderizado con PdfiumViewer, con zoom, navegación por páginas y salto a una página concreta.
- **Dibujo de la firma**: lienzo para dibujar con el ratón, con grosor configurable, borrado por trazos y su propio deshacer y rehacer.
- **Colocación de la firma**: clic sobre la página para colocarla, arrastre para moverla, rueda para cambiar el tamaño antes de soltarla y un tirador en la esquina para reescalarla después.
- **Cuadros de texto**: se añaden sobre la página, se mueven arrastrando, y cada uno tiene tamaño de fuente, color y botón de borrar.
- **Deshacer y rehacer**: Ctrl+Z y Ctrl+Y sobre la firma y sobre los cuadros de texto, incluyendo añadir, mover, redimensionar y borrar.
- **Abrir arrastrando**: sueltas un PDF sobre la ventana y se carga.
- **Exportación**: el PDF se genera con PdfPig, conservando el contenido original y la transparencia de la firma.

## Cómo se usa

1. Abre el PDF, con el botón o arrastrándolo sobre la ventana.
2. Dibuja la firma en la ventana de firma.
3. Haz clic en la página para colocarla; arrástrala para moverla y tira del tirador para reescalarla.
4. Añade los cuadros de texto que necesites.
5. Guarda el PDF.

## Stack

| Pieza | Qué uso |
|---|---|
| Lenguaje | C# / WPF sobre .NET 10 (`net10.0-windows`) |
| Núcleo | `Pdf_Signer.Core`, biblioteca `net10.0` sin dependencias de WPF |
| Renderizado PDF | PdfiumViewer (`Hueppauff.PdfiumViewer.Core` con el binario nativo x86_64) |
| Manipulación PDF | PdfPig |
| Pruebas | xUnit |
| Comandos de interfaz | `RelayCommand` propio |

## Estructura

```
Pdf_Signer.Core/Models/       lo que se estampa: firma, cuadros de texto y estado del editor
Pdf_Signer.Core/Placement/    geometria: colocacion, zoom y conversion de coordenadas
Pdf_Signer.Core/History/      historial de deshacer generico
Pdf_Signer.Core/Pdf/          lectura de la cabecera PNG y estampado con PdfPig
ViewModels/                   estado y comportamiento del editor
Views/                        XAML y fontaneria de raton y teclado
Services/                     renderizado con Pdfium y dialogos, detras de interfaces
Commands/                     implementacion de ICommand
tests/Pdf_Signer.Tests/       pruebas xUnit del nucleo
```

## Ejecutar

Necesitas el SDK de .NET 10 y Windows, porque WPF no es multiplataforma.

```bash
git clone https://github.com/idarkar3000/Pdf_Signer.git
cd Pdf_Signer
dotnet build Pdf_Signer.slnx
dotnet run --project Pdf_Signer.csproj
```

Las pruebas:

```bash
dotnet test tests/Pdf_Signer.Tests
```

También se puede abrir `Pdf_Signer.slnx` en Visual Studio con soporte para .NET 10 y ejecutar desde ahí.

## Sobre la arquitectura

Es MVVM. El estado y el comportamiento están en `MainViewModel`, y la vista solo traduce eventos de ratón y teclado a llamadas sobre él: no guarda estado propio.

`Pdf_Signer.Core` no depende de WPF. La geometría, el historial y el estampado usan tipos propios (`RelativePoint`, `PlacementRect`) y `byte[]`, así que se pueden probar sin abrir una ventana. El renderizado y los diálogos van detrás de `IPdfRenderService` e `IDialogService`, que el ViewModel recibe por constructor.

Un detalle que prefiero decir en vez de esconder: los PNG de la firma y de los cuadros de texto los produce la vista, porque es lo único que sabe convertir un control en imagen. El ViewModel los pide a través de un delegado.

## Pruebas y CI

68 pruebas xUnit sobre el núcleo: colocación de la firma y del texto (incluido el caso de una firma más grande que la página, que antes reventaba), zoom, historial de deshacer, lectura de la cabecera de un PNG y estampado real sobre un PDF de dos páginas, comprobando con PdfPig que la imagen cae solo en su página y en la posición esperada.

GitHub Actions compila y pasa las pruebas en `windows-latest` en cada push y en cada pull request.

## Limitaciones

- **Solo Windows.** Es WPF, no hay plan de portarlo.
- **La firma y los cuadros de texto se estampan como imagen**, no como texto seleccionable del PDF. Es el precio de conservar exactamente el color y la fuente que se ven en pantalla.
- **No es una firma digital criptográfica.** Estampa una firma manuscrita sobre el documento. No firma con certificado ni valida la identidad del firmante, así que no sustituye a una firma electrónica avanzada.

## Licencia

MIT. Mira [LICENSE](LICENSE).
