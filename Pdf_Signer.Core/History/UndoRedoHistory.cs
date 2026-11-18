namespace Pdf_Signer.Core.History;

/// <summary>
/// Historial de deshacer/rehacer generico. Guarda estados completos (no
/// comandos), que para un editor de este tamano es lo mas simple y lo que menos
/// se rompe.
/// </summary>
/// <typeparam name="T">Tipo del estado. Debe ser una referencia inmutable.</typeparam>
public sealed class UndoRedoHistory<T> where T : class
{
    private readonly Stack<T> _undo = new();
    private readonly Stack<T> _redo = new();

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public int UndoCount => _undo.Count;

    public int RedoCount => _redo.Count;

    /// <summary>
    /// Guarda un estado nuevo. Al hacerlo se descarta el rehacer: ya no hay
    /// "futuro" al que volver.
    /// </summary>
    public void Push(T state)
    {
        ArgumentNullException.ThrowIfNull(state);

        _undo.Push(state);
        _redo.Clear();
    }

    /// <summary>
    /// Deshace. Hay que pasar el estado actual para poder rehacer despues.
    /// Devuelve el estado al que hay que volver, o <c>null</c> si no hay nada
    /// que deshacer.
    /// </summary>
    public T? Undo(T current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (!CanUndo) return null;

        _redo.Push(current);
        return _undo.Pop();
    }

    /// <summary>Rehace. Mismo contrato que <see cref="Undo"/>.</summary>
    public T? Redo(T current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (!CanRedo) return null;

        _undo.Push(current);
        return _redo.Pop();
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
