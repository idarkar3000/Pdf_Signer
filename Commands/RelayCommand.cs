using System;
using System.Windows.Input;

namespace Pdf_Signer.Commands
{
    /// <summary>
    /// Comando de MVVM: envuelve un delegado para poder enlazarlo desde XAML.
    /// El reconsultado es manual (<see cref="RaiseCanExecuteChanged"/>) en vez de
    /// usar <c>CommandManager.RequerySuggested</c>, que reconsulta todos los
    /// comandos de la aplicacion en cada clic.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            ArgumentNullException.ThrowIfNull(execute);

            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute is null || _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
