namespace Pdf_Signer.Core.Models;

/// <summary>
/// Fotografia de todo lo que el usuario ha colocado sobre el documento.
/// Es inmutable a proposito: es lo que guarda el historial de deshacer, y una
/// copia no debe cambiar cuando el usuario sigue editando.
/// </summary>
public sealed class EditorState
{
    public static EditorState Empty { get; } = new();

    public SignatureStamp? Signature { get; init; }

    public IReadOnlyList<TextStamp> TextStamps { get; init; } = Array.Empty<TextStamp>();

    public bool IsEmpty => Signature is null && TextStamps.Count == 0;

    public EditorState WithSignature(SignatureStamp? signature) =>
        new() { Signature = signature, TextStamps = TextStamps };

    public EditorState WithTextStamps(IEnumerable<TextStamp> stamps) =>
        new() { Signature = Signature, TextStamps = stamps.ToArray() };
}
