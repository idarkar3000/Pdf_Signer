# Pdf_Signer

Aplicación de escritorio en C# y WPF sobre .NET 10 para visualizar documentos PDF y estampar firmas manuscritas encima, sin imprimir ni escanear nada.

La hice para firmar documentos sin pasar por la impresora. La firma se guarda de forma vectorial, así que el PDF resultante se puede ampliar sin que aparezcan píxeles.

## Qué hace

- **Visor**: renderizado con PdfiumViewer, con zoom, navegación por páginas y salto a una página concreta.
- **Dibujo de la firma**: lienzo para dibujar con el ratón, con grosor configurable y borrado por trazos.
- **Colocación**: la firma se arrastra y se reescala sobre el documento en tiempo real, con controles de tamaño y color.
- **Deshacer y rehacer**: Ctrl+Z y Ctrl+Y, tanto en el dibujo de la firma como en su posición sobre el PDF.
- **Abrir arrastrando**: sueltas un PDF sobre la ventana y se carga.
- **Exportación**: estampado vectorial con PdfPig, conservando la integridad del documento y la transparencia de la firma.

## Cómo se usa

1. Abre el PDF, con el botón o arrastrándolo sobre la ventana.
2. Dibuja la firma en la ventana de firma.
3. Colócala sobre el documento: arrastra para mover, tira de los controles para reescalar.
4. Exporta el PDF firmado.

## Stack

| Pieza | Qué uso |
|---|---|
| Lenguaje | C# / WPF sobre .NET 10 (`net10.0-windows`) |
| Renderizado PDF | PdfiumViewer (`Hueppauff.PdfiumViewer.Core` con el binario nativo x86_64) |
| Manipulación PDF | PdfPig |
| Comandos de interfaz | `RelayCommand` propio |

## Estructura

```
App.xaml, App.xaml.cs           arranque de la aplicación
Commands/RelayCommand.cs        implementación de ICommand
Views/MainWindow.xaml           visor, zoom, navegación y exportación
Views/SignatureWindow.xaml      lienzo de dibujo de la firma
Views/DraggableTextBlock.xaml   control de la firma colocable sobre el PDF
```

## Ejecutar

Necesitas el SDK de .NET 10 y Windows, porque WPF no es multiplataforma.

```bash
git clone https://github.com/idarkar3000/Pdf_Signer.git
cd Pdf_Signer
dotnet build
dotnet run
```

También se puede abrir `Pdf_Signer.slnx` en Visual Studio con soporte para .NET 10 y ejecutar desde ahí.

## Sobre la arquitectura

Las vistas están separadas y las acciones de la interfaz pasan por `RelayCommand`, pero **el estado sigue en el code-behind**: no hay ViewModels. Es el primer refactor pendiente, y está anotado como tal en vez de anunciar MVVM sin serlo.

## Limitaciones

- **Solo Windows.** Es WPF, no hay plan de portarlo.
- **Sin tests.** No hay suite automatizada; las pruebas han sido manuales.
- **Sin CI.**
- **No es una firma digital criptográfica.** Estampa una firma manuscrita sobre el documento. No firma con certificado ni valida la identidad del firmante, así que no sustituye a una firma electrónica avanzada.

## Licencia

MIT. Mira [LICENSE](LICENSE).
