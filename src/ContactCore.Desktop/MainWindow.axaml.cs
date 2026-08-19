using Avalonia.Controls;
using Avalonia.Input;

namespace ContactCore.Desktop;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { vm.NewContactCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { if (vm.SaveCommand.CanExecute(null)) vm.SaveCommand.Execute(null); e.Handled = true; }
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { vm.FocusSearchRequested?.Invoke(); e.Handled = true; }
        if (e.Key == Key.Escape) { vm.CancelEditCommand.Execute(null); e.Handled = true; }
    }
}
