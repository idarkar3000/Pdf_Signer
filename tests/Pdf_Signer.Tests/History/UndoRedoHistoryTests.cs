using Pdf_Signer.Core.History;

namespace Pdf_Signer.Tests.History;

public class UndoRedoHistoryTests
{
    [Fact]
    public void Empieza_Vacio()
    {
        UndoRedoHistory<string> history = new();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Deshacer_Devuelve_El_Estado_Anterior()
    {
        UndoRedoHistory<string> history = new();

        history.Push("antes");

        Assert.True(history.CanUndo);
        Assert.Equal("antes", history.Undo(current: "ahora"));
        Assert.True(history.CanRedo);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Rehacer_Devuelve_El_Estado_Siguiente()
    {
        UndoRedoHistory<string> history = new();
        history.Push("antes");
        history.Undo(current: "ahora");

        Assert.Equal("ahora", history.Redo(current: "antes"));
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Varios_Pasos_Van_Y_Vienen_En_Orden()
    {
        UndoRedoHistory<string> history = new();
        history.Push("uno");
        history.Push("dos");

        Assert.Equal("dos", history.Undo(current: "tres"));
        Assert.Equal("uno", history.Undo(current: "dos"));
        Assert.Equal("dos", history.Redo(current: "uno"));
        Assert.Equal("tres", history.Redo(current: "dos"));
    }

    [Fact]
    public void Guardar_Un_Estado_Nuevo_Descarta_El_Rehacer()
    {
        UndoRedoHistory<string> history = new();
        history.Push("antes");
        history.Undo(current: "ahora");

        Assert.True(history.CanRedo);

        history.Push("otra cosa");

        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Sin_Historial_No_Devuelve_Nada()
    {
        UndoRedoHistory<string> history = new();

        Assert.Null(history.Undo(current: "ahora"));
        Assert.Null(history.Redo(current: "ahora"));
    }

    [Fact]
    public void Limpiar_Lo_Deja_Todo_Vacio()
    {
        UndoRedoHistory<string> history = new();
        history.Push("uno");
        history.Undo(current: "dos");

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    [Fact]
    public void Rechaza_Estados_Nulos()
    {
        UndoRedoHistory<string> history = new();

        Assert.Throws<ArgumentNullException>(() => history.Push(null!));
        Assert.Throws<ArgumentNullException>(() => history.Undo(null!));
        Assert.Throws<ArgumentNullException>(() => history.Redo(null!));
    }
}
