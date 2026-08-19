using Avalonia.Controls;
using Avalonia.Input;

namespace ContactCore.Desktop;

public sealed partial class MainWindow : Window
{
    private MainWindowViewModel? _wiredViewModel;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_wiredViewModel is not null)
            _wiredViewModel.FocusSearchRequested = null;

        _wiredViewModel = DataContext as MainWindowViewModel;
        if (_wiredViewModel is not null)
            _wiredViewModel.FocusSearchRequested = () => SearchBox.Focus();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_wiredViewModel is not null)
            _wiredViewModel.FocusSearchRequested = null;
        _wiredViewModel = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            vm.NewContactCommand.Execute(null);
            e.Handled = true;
        }
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (vm.SaveCommand.CanExecute(null)) vm.SaveCommand.Execute(null);
            e.Handled = true;
        }
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            vm.FocusSearchRequested?.Invoke();
            e.Handled = true;
        }
        if (e.Key == Key.Escape)
        {
            vm.CancelEditCommand.Execute(null);
            e.Handled = true;
        }
    }
}
